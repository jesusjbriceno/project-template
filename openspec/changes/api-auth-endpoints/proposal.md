# Proposal: API Authentication Endpoints

## Intent

The backend has seeded users but zero authentication endpoints — no login, no token issuance, no session management. This change delivers login, refresh, and logout endpoints with JWT access tokens + HttpOnly cookie refresh-token rotation, unlocking the admin dashboard as first authenticated consumer.

## Scope

### In Scope
- `POST /auth/login` — email+password → access token (JSON) + refresh token cookie
- `POST /auth/refresh` — valid cookie → new access token + rotated refresh cookie
- `POST /auth/logout` — revoke entire token family, clear cookie
- JWT HS256 issuance/validation + real `UserSession` via `IHttpContextAccessor`
- CQRS auth handlers: `LoginCommandHandler`, `RefreshTokenCommandHandler`, `LogoutCommandHandler`
- Configurable password policy (default: min 12, uppercase, lowercase, digit, special)
- Refresh token lifetime: 7 days default, configurable via `Jwt__RefreshTokenDays`
- Family-based rotation with reuse detection → full family revocation + security log
- FluentValidation on all auth commands; Result pattern, no exceptions for control flow

### Out of Scope
Self-registration, account lockout, login rate limiting, per-device token families, RS256 (future), password reset, email verification, MFA, social login, audit logging.

## Capabilities

### New Capabilities
- `api-authentication`: Login/refresh/logout endpoints with JWT access tokens (HS256, 15-min) and refresh-token rotation via HttpOnly/Secure/SameSite=Strict cookies. Spans JWT infrastructure, auth use cases, and AuthController.

### Modified Capabilities
- `application-layer`: Adds `IJwtTokenService` abstraction (issuance + validation), concrete auth handlers for login/refresh/logout, validators, and `ErrorCodes.Auth` — fulfilling the foundation spec's deferred handler-level behavior.

## Approach

Three infrastructure→application→API slices (force-chained, ≤400 lines each):
1. **JWT Infrastructure**: `JwtTokenService` (HS256), `UserSession`, `AddJwtBearer()` wiring, JWT options POCO
2. **Application**: Commands/handlers with `Result`, token reuse → `RevokeFamilyAsync`, password validation
3. **API Controller**: `AuthController`, DTOs with explicit operators, cookie management, integration tests

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Project.Infrastructure/Security/` | New | `JwtTokenService`, `UserSession`, `JwtOptions` |
| `Project.Infrastructure/DependencyInjection.cs` | Modified | Auth middleware + DI wiring |
| `Project.Application.Abstractions/Security/` | New | `IJwtTokenService` contract |
| `Project.Application/Auth/` | New | Commands, handlers, validators, `TokenPairDto` |
| `Project.Application/ErrorCodes.cs` | Modified | `Auth` subclass (6 error codes) |
| `Project.Api.Controllers/AuthController.cs` | New | Login, Refresh, Logout actions |
| `Project.Api.Controllers/Program.cs` | Modified | `AddControllers()`, auth pipeline |
| `Project.*Tests/` | New | Unit + application + integration tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| JWT secret leaked | High | Env var only; `.env` gitignored; `.env.example` template |
| Refresh token CSRF | High | SameSite=Strict, HttpOnly, Secure, path `/auth/refresh` |
| Reuse detection missed | Medium | `RefreshTokenReuseSignalException` + integration test |
| UserSession breaks DI | Medium | `AddHttpContextAccessor()`; audit existing tests |

## Rollback Plan

1. Remove auth middleware from `Program.cs`
2. Re-register `NullUserSession` as `IUserSession`
3. Delete `AuthController`, auth handlers, `JwtTokenService`, `UserSession`
4. Revert `DependencyInjection.cs` auth additions
5. Strip JWT env vars from `.env.example`

No database rollback needed — `refresh_tokens` table already exists.

## Dependencies

- `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet
- Existing `User`, `RefreshToken`, `IPasswordHasher`, `ITokenService`, repository interfaces
- Seeded Superadmin user from MigrationService

## Success Criteria

- [ ] `POST /auth/login` → 200 + access token + cookie for valid seeded credentials
- [ ] `POST /auth/refresh` → rotated tokens; reused revoked token → 401 + family revoked
- [ ] `POST /auth/logout` → 204 + cookie cleared, entire family revoked
- [ ] Generic error messages (no email enumeration); HttpOnly cookies; no raw tokens in logs
- [ ] All tests pass (unit + application + integration) under TDD discipline
- [ ] Zero secrets committed; `.env.example` documents required vars
