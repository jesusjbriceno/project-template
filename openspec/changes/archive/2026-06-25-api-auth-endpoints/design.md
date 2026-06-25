# Design: API Authentication Endpoints

## Technical Approach

Three infrastructure → application → API slices as force-chained PRs (≤400 LOC each): JWT HS256 + cookie wiring (slice 1), CQRS handlers (slice 2), `AuthController` + integration tests (slice 3). Domain invariants are proven; this change wires the running API to them.

## Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| `IJwtTokenService` | App Abstractions; concrete in Infra | Key material stays in Infra; spec mandates interface in App |
| `UserSession` | Real impl via `IHttpContextAccessor` → claims | `NullUserSession` blocks every authed endpoint |
| Refresh token shape | 32 random bytes → base64url raw to client; SHA-256 → 64-char hex stored | Matches `RefreshToken.Create`; OWASP standard |
| `ITokenService` impl | Thin `TokenService` adapter → `IRefreshTokenRepository.RevokeFamilyAsync` | Preserves boundary for future audit/metrics |
| Controllers vs endpoints | `AuthController` under `Project.Api.Controllers/Controllers/` | AGENTS.md: controllers preferred |
| Command handler contract | `ICommandHandler<TCommand, TResult>` for value-returning handlers; `ICommandHandler<TCommand>` for void | `LoginCommandHandler` and `RefreshTokenCommandHandler` implement the typed variant; `LogoutCommandHandler` implements the void variant |
| User with roles lookup | New `IUserRepository.GetByEmailWithRolesAsync` | Needed for JWT `roles[]`; `UserRole` → `Role` is navigationless |
| Password policy | Static `PasswordPolicy.IsStrongEnough` in App; same 401 on failure | Single source; aligns with `SuperadminCredentialValidator.MinimumPasswordLength = 12` |
| Startup config | `AddOptions<JwtOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` | Built-in; fails fast |
| Cookie `Secure` | `SameAsRequest` in Dev, `Always` otherwise | Localhost HTTP dev; production behind HTTPS |

## Data Flow

```
[Browser] POST /auth/login {email,pw} → AuthController
  → LoginCommandHandler: GetByEmailWithRolesAsync → PasswordHasher.Verify
      → PasswordPolicy.IsStrongEnough → RefreshToken.Create+Add
      → IJwtTokenService.GenerateAccessToken
  → Result<TokenPairDto> → 200 { accessToken, expiresIn }
      Set-Cookie: refreshToken=<raw>; HttpOnly; Secure; SameSite=Strict;
                 Path=/auth; Max-Age=Jwt__RefreshTokenDays*86400
```

Refresh: cookie → `GetByTokenHashAsync` → `Rotate` (catches `RefreshTokenReuseSignalException` → `ITokenService.RevokeFamilyAsync`) → new access + rotated cookie. Logout: lookup → revoke family → clear cookie → 204.

## File Changes

| Layer | Files |
|-------|-------|
| **App create** | `Abstractions/Security/IJwtTokenService.cs` |
| **App modify** | `Common/ErrorCodes.cs` (+ `Auth`); `Abstractions/Persistence/IUserRepository.cs` (+ `GetByEmailWithRolesAsync`) |
| **App use cases** (12 files in `Auth/`) | `TokenPairDto`, `PasswordPolicy`, `Login/RefreshToken/Logout` × `Command`/`Handler`/`Validator` |
| **Infra create** | `Security/{JwtOptions,JwtTokenService,UserSession,TokenService}.cs` |
| **Infra modify** | `Project.Infrastructure.csproj` (+ JwtBearer + Http.Abstractions); `DependencyInjection.cs` (replace `NullUserSession`; add `IHttpContextAccessor`, `IOptions<JwtOptions>`, `IJwtTokenService`, `ITokenService`, auth wiring); `UserRepository.cs` (`GetByEmailWithRolesAsync` w/ `Include` + role join) |
| **Infra delete** | `Security/NullUserSession.cs` |
| **API create** | `Controllers/AuthController.cs`; `Contracts/{LoginRequest,TokenResponse}.cs` (explicit operators) |
| **API modify** | `Program.cs` (controllers, app, auth middleware, `Jwt` binding); `appsettings.json` (+ `Jwt`) |
| **Ops/docs modify** | `docker-compose.yml`, `.env.example` (+ `Jwt__Secret` ≥32 bytes, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshTokenDays`); `README.md` |

## Interfaces / Contracts

```csharp
public interface IJwtTokenService
{
    (string Token, TimeSpan Lifetime) GenerateAccessToken(User user, IReadOnlyCollection<Role> roles);
    (string Raw, string Hash) GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
}
public sealed record TokenPairDto(string AccessToken, int ExpiresInSeconds, string RefreshToken);
// RefreshToken is the raw value for the API layer to set the HttpOnly cookie
public sealed record LoginCommand(string Email, string Password) : ICommand;
public sealed record RefreshTokenCommand(string RefreshTokenRaw) : ICommand;
public sealed record LogoutCommand(string RefreshTokenRaw) : ICommand;

// Infrastructure POCO, bound from "Jwt" config
public sealed class JwtOptions
{
    [Required, MinLength(32)] public string Secret { get; set; } = "";
    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";
    public int RefreshTokenDays { get; set; } = 7;
}
```

## Testing Strategy

| Layer | What | How |
|-------|------|-----|
| Unit | `PasswordPolicy`; `JwtTokenService` round-trip / tampered / expired; `UserSession` claim mapping; all three `*CommandValidator`s | Pure xUnit |
| Application | `Login` (success, wrong pwd, blocked, missing user, weak pwd → same code); `Refresh` (rotation, expired → `AUTH_TOKEN_EXPIRED`, revoked → reuse → family revoked, missing); `Logout` (success, missing) | Hand-rolled fakes matching `TokenServiceStub` pattern |
| Integration | `AuthEndpointsTests` (login+401+cookie; refresh rotation+reuse; logout 204+family revoked); `AuthMiddlewareTests` (401 on missing/invalid token) | `WebApplicationFactory<Program>` + factory override for `Jwt:Secret`; shared `PostgresCollection`; transactional isolation |

Tests ship with the work unit they verify (work-unit-commits).

## Migration / Rollout

- **Schema**: `refresh_tokens` table exists; Slice 2 adds `UserId` column via migration `20260624120000_AddRefreshTokenUserId`.
- **Env**: ops generate `Jwt__Secret` (≥32 random bytes). Rotate by redeploy; access tokens expire ≤15 min, refresh tokens become unverifiable.
- **Dev**: `Secure` relaxed via `CookieSecurePolicy.SameAsRequest`.
- **Rollback**: revert `Program.cs`; re-register `NullUserSession`; delete auth files. Run `Down` migration for `20260624120000_AddRefreshTokenUserId` to drop `UserId` column (migration deletes existing refresh tokens in `Up()` — no data to restore on rollback).

## Chained PR Plan

| # | Branch | Target | Scope | LOC |
|---|--------|--------|-------|-----|
| 1 | `01-jwt-infrastructure` | `develop` | Service + `UserSession` + DI; env; unit tests | ~300 |
| 2 | `02-use-cases` | #1 | Handlers/validators, `ErrorCodes.Auth`, app tests | ~380 |
| 3 | `03-api-controller` | #2 | `AuthController`, DTOs, integration tests | ~330 |

Build stays green: #1 wires DI (no controller), #2 has handlers (no controller), #3 lights endpoints.

## Open Questions

- [ ] Cookie inspection in integration tests: parse `Set-Cookie` via `Headers.GetValues("Set-Cookie")` — confirm in apply.
- [ ] `IUserRepository.GetByEmailWithRolesAsync` is an interface addition; covered by `application-layer` delta.
- [ ] Logout with no cookie returns 400 `AUTH_REFRESH_TOKEN_MISSING` per spec — distinguishes "no session" without exposing state.

## Next Step

Ready for `sdd-tasks`.
