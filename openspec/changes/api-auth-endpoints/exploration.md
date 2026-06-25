# Exploration: API Auth Endpoints

## Current State

The backend has four layers built and archived:

| Layer | Status | Relevant to Auth |
|-------|--------|------------------|
| **Domain** | ✅ Archived | `User`, `Role`, `Permission`, `RefreshToken`, `Email`, `IPasswordHasher`, `ITokenService` contracts, `IUserSession` contract, `RefreshTokenReuseSignalException` |
| **Application** | ✅ Foundation archived | `Result`/`Result<T>`, CQRS contracts (`ICommand`, `IQuery<T>`, handlers), repository interfaces (`IUserRepository`, `IRefreshTokenRepository`), `IBaseRepository<TEntity,TId>`, `ErrorCodes` |
| **Infrastructure** | ✅ Archived | `ApplicationDbContext`, `UserRepository`, `RefreshTokenRepository`, `BCryptPasswordHasher`, `NullUserSession`, `DependencyInjection`, PostgreSQL Testcontainers |
| **MigrationService** | ✅ Archived | Seeds `Superadmin` + `User` system roles, permission catalog, initial superadmin user with bcrypt hash |
| **API Controllers** | ⚠️ Scaffold only | `Program.cs` with `/health` only; placeholder comments for `AddAuthentication()`, `UseAuthentication()`, `UseAuthorization()` |
| **API Endpoints** | ⚠️ Empty classlib | `Project.Api.Endpoints.csproj` exists, zero `.cs` files |

**Critical gap**: No Application-layer use-case handlers exist. No JWT issuance/validation infrastructure exists. The `ITokenService` interface only exposes `RevokeFamilyAsync` — it intentionally does NOT expose token issuance or validation per Application-layer security boundary rules. The `NullUserSession` returns null/unauthenticated for all properties.

**The API layer is the only unbuilt layer.** This change will touch:
- Infrastructure: add JWT token issuance/validation services, replace `NullUserSession` with real `UserSession`
- Application: add auth use-case commands, handlers, validators, and error codes
- API Controllers: add `AuthController` with login/refresh/logout actions
- Integration tests: end-to-end auth flow tests against real PostgreSQL + WebApplicationFactory

## Affected Areas

- `apps/api/src/Project.Infrastructure/Security/` — needs JWT token service + real `UserSession` implementation
- `apps/api/src/Project.Infrastructure/DependencyInjection.cs` — needs `AddAuthentication()` / `AddJwtBearer()` wiring
- `apps/api/src/Project.Application/` — needs auth commands, handlers, validators, and error codes
- `apps/api/src/Project.Api.Controllers/Program.cs` — needs auth middleware pipeline + controller services
- `apps/api/src/Project.Api.Controllers/` — needs `AuthController.cs`
- `apps/api/tests/Project.ApplicationTests/` — needs handler unit tests
- `apps/api/tests/Project.IntegrationTests/` — needs endpoint integration tests
- `openspec/specs/application-layer/spec.md` — may need delta for auth use-case contracts
- `.env.example` — needs `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshTokenDays`

---

## 1. Auth Endpoints for the First Backend Slice

### Recommended first-slice endpoints

| Endpoint | Method | Purpose | In first slice? |
|----------|--------|---------|-----------------|
| `POST /auth/login` | Login | Exchange email+password for access token + refresh token cookie | **Yes** |
| `POST /auth/refresh` | Refresh | Exchange valid refresh token cookie for new access token + rotate refresh token | **Yes** |
| `POST /auth/logout` | Logout | Revoke current refresh token family, clear cookie | **Yes** |
| `POST /auth/register` | Register | Create new user account | **Defer — product question** |
| `GET /auth/me` | Self | Return current user identity + roles | **Defer to user-management slice** |
| `POST /auth/revoke` | Admin revoke | Revoke all tokens for a user (admin) | **Defer to RBAC slice** |

### Registration: defer or include?

**Recommendation: Defer self-registration to a future user-management slice.**

Rationale:
- The seed data already creates a Superadmin user. The first consumer of auth is the frontend admin dashboard.
- Self-registration opens product questions (email verification, approval workflows, default role assignment) that are not yet answered.
- Admin-driven user creation via a `POST /users` endpoint (future RBAC slice) satisfies immediate needs.
- If the product requires public self-registration, it can be added as a dedicated slice with its own validation rules (email verification, CAPTCHA, etc.).

**Exception**: If the user explicitly confirms public self-registration is required for MVP, include a minimal `POST /auth/register` in Slice 3 with basic email+password validation and default `User` role assignment.

---

## 2. Missing Application Use Cases

The Application layer has **zero handlers today**. These are needed:

### Commands

| Command | Handler | Returns | Dependencies |
|---------|---------|---------|--------------|
| `LoginCommand` | `LoginCommandHandler` | `Result<TokenPairDto>` | `IUserRepository`, `IPasswordHasher`, `IJwtTokenService` (new) |
| `RefreshTokenCommand` | `RefreshTokenCommandHandler` | `Result<TokenPairDto>` | `IRefreshTokenRepository`, `IClock`, `IJwtTokenService` (new) |
| `LogoutCommand` | `LogoutCommandHandler` | `Result` | `IRefreshTokenRepository`, `IClock` |

### Queries

| Query | Handler | Returns | Dependencies |
|-------|---------|---------|--------------|
| `GetCurrentUserQuery` | `GetCurrentUserQueryHandler` | `Result<UserDto>` | `IUserRepository`, `IUserSession` |

> `GetCurrentUserQuery` is deferred to the user-management slice but listed for completeness.

### New Application abstractions needed

The existing `ITokenService` only exposes `RevokeFamilyAsync`. The Application layer needs a **new** abstraction for token issuance:

```csharp
public interface IJwtTokenService  // or ITokenIssuer
{
    string GenerateAccessToken(User user, IReadOnlyCollection<Role> roles);
    string GenerateRefreshToken();  // returns raw token string (64 bytes → hex)
    (UserId? userId, IReadOnlyCollection<string> roles) ValidateAccessToken(string token);
}
```

> **Design decision**: Keep `ITokenService` (family revocation) separate from `IJwtTokenService` (issuance/validation). The original `ITokenService` was designed to prevent raw token handling from leaking into Application. Token issuance is an Infrastructure concern, but Application needs to call it. The compromise: `IJwtTokenService` lives in `Project.Application.Abstractions.Security` and is implemented in Infrastructure.

### Validators

- `LoginCommandValidator` — Email format, password not empty
- `RefreshTokenCommandValidator` — Refresh token cookie present (API-layer concern; validator may be no-op if command carries the raw token)
- `LogoutCommandValidator` — Refresh token cookie present

### Error Codes

New error codes needed in `ErrorCodes`:

```csharp
public static class Auth
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string UserBlocked = "AUTH_USER_BLOCKED";
    public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
    public const string TokenRevoked = "AUTH_TOKEN_REVOKED";
    public const string TokenReuseDetected = "AUTH_TOKEN_REUSE_DETECTED";
    public const string RefreshTokenMissing = "AUTH_REFRESH_TOKEN_MISSING";
}
```

---

## 3. Controllers vs Endpoints

### Project rules

From `AGENTS.md`:
> **Controllers preferred by default. Minimal API endpoints go under `Endpoints/`, never in `Program.cs`.**

### Current structure

- `Project.Api.Controllers` — `Microsoft.NET.Sdk.Web`, references Application + Infrastructure, contains `Program.cs`
- `Project.Api.Endpoints` — `Microsoft.NET.Sdk` (classlib), references Application + Infrastructure, **empty**

### Recommendation: Use Controllers

| Factor | Controllers | Minimal Endpoints |
|--------|-------------|-------------------|
| Project rule | **Preferred by default** | Allowed only for minimal cases |
| Auth complexity | Multiple related actions (login, refresh, logout) with shared cookie logic, JWT configuration, and ProblemDetails mapping | Would require a custom `AuthModule` class — more boilerplate than a controller |
| Testability | `WebApplicationFactory` + `HttpClient` tests are straightforward | Same, but controller tests can also target the class directly |
| Existing patterns | No existing API code to follow | No existing endpoint modules to follow |
| Team familiarity | Standard ASP.NET Core pattern | Less familiar to most .NET teams |

**Decision**: Add `AuthController` to `Project.Api.Controllers`.

```csharp
[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    // POST auth/login
    // POST auth/refresh
    // POST auth/logout
}
```

The `Project.Api.Endpoints` project should remain empty for now. It can be used later for truly minimal endpoints (e.g., `GET /health` v2, webhooks) that don't fit the controller pattern. Do not delete it — it's part of the project structure.

---

## 4. JWT / Refresh-Token Security Behaviors

These behaviors MUST be specified in the design/spec phase:

### Access Token

| Attribute | Spec |
|-----------|------|
| Algorithm | HS256 (symmetric) or RS256 (asymmetric) — **decision needed** |
| Lifetime | 15 minutes (`AGENTS.md` mandate) |
| Claims | `sub` (UserId), `email`, `roles` (array of role names), `jti` (unique token ID) |
| Issuer | Configured `Jwt__Issuer` |
| Audience | Configured `Jwt__Audience` |
| Storage | Sent in response body as JSON field `accessToken` |

### Refresh Token

| Attribute | Spec |
|-----------|------|
| Format | Cryptographically random 64-byte array → SHA-256 hex hash stored; **raw token sent to client** |
| Lifetime | 7 days (configurable via `Jwt__RefreshTokenDays`) |
| Storage | HttpOnly, Secure, SameSite=Strict cookie (`refreshToken`) |
| Rotation | On every refresh: old token revoked, new token issued, new cookie set |
| Family ID | Guid shared across all tokens in a rotation chain |
| Reuse detection | If a revoked token is presented → revoke **entire family** → clear cookie → force re-login |
| Hash storage | Only SHA-256 hex hash (64 chars) stored in `RefreshToken.TokenHash` |

### Cookie Behavior

```
Set-Cookie: refreshToken=<raw-token>; HttpOnly; Secure; SameSite=Strict; Max-Age=604800; Path=/auth/refresh
```

- `Secure` requires HTTPS in production; development may need exception handling
- `SameSite=Strict` prevents CSRF on refresh/logout
- `Path=/auth/refresh` limits cookie scope (login sets it; refresh reads it; logout clears it)

### Logout Behavior

1. Read refresh token from cookie
2. Hash it, look up in database
3. If found → revoke the **entire family** (not just this token) as a security measure
4. Clear the `refreshToken` cookie (set Max-Age=0)
5. Return 204 No Content

> **Rationale**: If a user explicitly logs out, we assume the session may be compromised. Revoking the entire family ensures all devices sharing that rotation chain are signed out.

### Error Handling

| Scenario | HTTP Status | Error Code | Behavior |
|----------|-------------|------------|----------|
| Invalid credentials | 401 | `AUTH_INVALID_CREDENTIALS` | Generic message; do not reveal whether email exists |
| User deactivated | 401 | `AUTH_USER_BLOCKED` | Same generic message as invalid credentials (OWASP) |
| Refresh token missing | 400 | `AUTH_REFRESH_TOKEN_MISSING` | Cookie not present or empty |
| Refresh token expired | 401 | `AUTH_TOKEN_EXPIRED` | Clear cookie, force re-login |
| Refresh token revoked | 401 | `AUTH_TOKEN_REVOKED` | Clear cookie, force re-login |
| Refresh token reuse | 401 | `AUTH_TOKEN_REUSE_DETECTED` | Revoke family, clear cookie, log security event |

---

## 5. Testing Strategy (Strict TDD)

Under `strict_tdd: true` in `openspec/config.yaml`, every behavior requires a failing test first.

### Unit Tests (`Project.UnitTests`)

| Target | What to test | Est. lines |
|--------|--------------|------------|
| `LoginCommandValidator` | Valid email format, empty password rejected, null command rejected | 60-80 |
| `RefreshTokenCommandValidator` | Placeholder validation (command structure only) | 20-30 |
| `LogoutCommandValidator` | Placeholder validation | 20-30 |
| Domain auth edge cases | `User.IsBlocked` behavior, `RefreshToken.IsActive` / `IsExpired` / `IsReuseSignal` | 40-60 |
| **Unit subtotal** | | **140-200** |

### Application Tests (`Project.ApplicationTests`)

| Target | What to test | Est. lines |
|--------|--------------|------------|
| `LoginCommandHandler` | Success path, invalid credentials, blocked user, missing user, password hash verification | 120-160 |
| `RefreshTokenCommandHandler` | Success rotation, expired token, revoked token (reuse signal → family revocation), missing token | 120-160 |
| `LogoutCommandHandler` | Success revocation, missing token, already-revoked token | 60-80 |
| `IJwtTokenService` contract validation | Interface shape (no concrete tests — interface only) | 20-30 |
| **Application subtotal** | | **320-430** |

### Integration Tests (`Project.IntegrationTests`)

| Target | What to test | Est. lines |
|--------|--------------|------------|
| `POST /auth/login` | 200 + accessToken + cookie, 401 invalid credentials, 401 blocked user | 80-100 |
| `POST /auth/refresh` | 200 + new accessToken + new cookie, 401 expired, 401 revoked, 400 missing cookie | 80-100 |
| `POST /auth/logout` | 204 + cookie cleared, 400 missing cookie | 40-60 |
| Auth middleware | 401 on protected endpoint with missing/invalid access token | 40-60 |
| **Integration subtotal** | | **240-320** |

**Total test estimate**: 700-950 lines. This is a **major driver** for the 400-line review budget — tests alone exceed it.

> **Mitigation**: Tests travel with the behavior they verify (work-unit-commits rule). Each chained PR slice includes its own tests, so no single PR carries all 950 test lines.

---

## 6. Slice Boundaries for Force-Chained PRs

The total implementation is estimated at **1200-1600 lines** (Infrastructure + Application + API + tests). Under the 400-line budget and `force-chained` strategy, this requires **4 chained PRs**.

### Slice 1: JWT Infrastructure & UserSession
**Branch**: `feature/api-auth-endpoints-01-jwt-infrastructure`
**Target**: `develop` (or tracker branch)
**Scope**:
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` package to `Project.Infrastructure`
- Implement `JwtTokenService : IJwtTokenService` (issuance + validation)
- Implement `UserSession : IUserSession` (extract from `HttpContext.User` claims)
- Update `DependencyInjection.cs`: `AddAuthentication().AddJwtBearer()`, `AddAuthorization()`, replace `NullUserSession` with `UserSession`
- Add JWT settings POCO + bind from configuration
- Update `.env.example` with `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`
- Integration tests: `JwtTokenService` round-trip (issue → validate), `UserSession` claim extraction
**Est. changed lines**: 250-350
**Tests**: 80-120

### Slice 2: Application Auth Use Cases
**Branch**: `feature/api-auth-endpoints-02-use-cases`
**Target**: `feature/api-auth-endpoints-01-jwt-infrastructure`
**Scope**:
- `LoginCommand`, `LoginCommandHandler`, `LoginCommandValidator`
- `RefreshTokenCommand`, `RefreshTokenCommandHandler`, `RefreshTokenCommandValidator`
- `LogoutCommand`, `LogoutCommandHandler`, `LogoutCommandValidator`
- `TokenPairDto` (accessToken string)
- New `ErrorCodes.Auth` constants
- Application tests for all three handlers (mocked repositories + `IJwtTokenService` stub)
**Est. changed lines**: 300-400
**Tests**: 320-430

### Slice 3: API Auth Controller & Middleware
**Branch**: `feature/api-auth-endpoints-03-api-controller`
**Target**: `feature/api-auth-endpoints-02-use-cases`
**Scope**:
- `AuthController` with `Login`, `Refresh`, `Logout` actions
- Request/response DTOs (`LoginRequest`, `TokenResponse`) with explicit conversion operators (no AutoMapper)
- `Program.cs`: `AddControllers()`, `UseAuthentication()`, `UseAuthorization()`, `AddApplication()` extension
- Cookie configuration in controller actions (HttpOnly, Secure, SameSite)
- Integration tests: end-to-end login/refresh/logout against PostgreSQL Testcontainers + WebApplicationFactory
**Est. changed lines**: 250-350
**Tests**: 240-320

### Slice 4: Registration (Optional — product-dependent)
**Branch**: `feature/api-auth-endpoints-04-registration`
**Target**: `feature/api-auth-endpoints-03-api-controller`
**Scope**:
- `RegisterCommand`, `RegisterCommandHandler`, `RegisterCommandValidator`
- `POST /auth/register` controller action
- Default role assignment (`User` system role)
- Integration tests for registration flow
**Est. changed lines**: 150-250
**Tests**: 100-150

> **Note**: If registration is deferred, Slice 4 is omitted and the chain ends at Slice 3.

### Dependency Diagram

```
develop
  └── feature/api-auth-endpoints-01-jwt-infrastructure   ← PR #1 📍
        └── feature/api-auth-endpoints-02-use-cases      ← PR #2
              └── feature/api-auth-endpoints-03-api-controller  ← PR #3
                    └── feature/api-auth-endpoints-04-registration  ← PR #4 (optional)
```

### Review Budget Check

| Slice | Est. Lines | Budget |
|-------|------------|--------|
| #1 JWT Infrastructure | 250-350 | ✅ ≤400 |
| #2 Use Cases | 300-400 | ✅ ≤400 |
| #3 API Controller | 250-350 | ✅ ≤400 |
| #4 Registration | 150-250 | ✅ ≤400 |

---

## 7. Open Product / Business Questions

These must be answered before `sdd-propose` to avoid scope creep:

### High Priority (blocks design)

1. **Self-registration**: Is public self-registration required for MVP, or is admin-driven user creation sufficient?
   - If yes: need email verification flow? CAPTCHA? Default role = `User`?
   - If no: defer to user-management slice.

2. **Password policy**: What are the minimum password requirements?
   - Current: BCrypt with cost factor 12, no length validation in Domain.
   - Need: min length? complexity (upper/lower/digit/symbol)?
   - The `SuperadminCredentialValidator` enforces 12 chars for superadmin — should this be the global minimum?

3. **Account lockout**: After N failed login attempts, should the account be temporarily locked?
   - If yes: how many attempts? lockout duration? where to store attempt counters?
   - If no: brute-force risk accepted?

4. **JWT algorithm**: Symmetric (HS256) or asymmetric (RS256)?
   - HS256: simpler, single secret, suitable for monolith.
   - RS256: better for microservices/federation, requires key management.
   - This affects `JwtTokenService` implementation and secret storage.

### Medium Priority (can be decided in design phase)

5. **Refresh token lifetime**: 7 days? 30 days? Configurable per environment?
6. **Multi-device sessions**: Should a user have multiple active token families (one per device), or a single global family?
   - Multiple families: better UX, harder to revoke globally.
   - Single family: simpler security model, logout-on-any-device signs out all devices.
7. **Rate limiting**: Should login/refresh endpoints have per-IP rate limiting?
8. **CORS policy**: What origins are allowed for the API? (Needed for cookie-based refresh to work cross-origin.)

### Low Priority (future phases)

9. **Email verification**: Required for self-registration? Token-based link?
10. **Password reset**: Self-service via email? Admin-only reset?
11. **MFA / 2FA**: TOTP? SMS? WebAuthn?
12. **Social login**: Google, GitHub, etc.?
13. **Audit logging**: Should every login/logout/refresh be logged to a security audit table?

---

## Recommendation

**Proceed with 3-slice chained PRs** (4 if self-registration is confirmed):

1. **Slice 1 — JWT Infrastructure**: Token issuance/validation + real UserSession + auth middleware wiring.
2. **Slice 2 — Application Use Cases**: Login, Refresh, Logout handlers with TDD.
3. **Slice 3 — API Controller**: AuthController + DTOs + integration tests.

**Defer registration** unless the user explicitly confirms it is MVP-required.

**Default decisions** (can be overridden in proposal):
- HS256 symmetric JWT (simpler for monolith)
- 7-day refresh token lifetime
- Single token family per user (logout signs out all devices)
- No account lockout in first slice (can be added later)
- Password minimum length = 12 characters (align with superadmin validator)

---

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| **JWT secret management** | High | Secret must be injected via env var, never committed. `.env.example` template only. Docker Compose reads from `.env` file. |
| **Refresh token cookie CSRF** | High | `SameSite=Strict` + `HttpOnly` + `Secure`. Clear cookie on logout. |
| **Token reuse detection complexity** | Medium | `RefreshToken.Rotate()` throws `RefreshTokenReuseSignalException`. Handler must catch, call `RevokeFamilyAsync`, return failure. Integration test must verify family-wide revocation. |
| **NullUserSession → real UserSession breaking change** | Medium | `NullUserSession` is registered as `IUserSession`. Replacing it with `UserSession` (which depends on `IHttpContextAccessor`) requires `AddHttpContextAccessor()` in DI. Any existing tests relying on `NullUserSession` must be updated. |
| **Testcontainers performance** | Low | Integration tests spin PostgreSQL per collection. Auth tests add ~3-5 seconds per test class. Use `PostgresFixture` shared across collection (already implemented). |
| **Access token in response body** | Medium | Storing access tokens in localStorage is vulnerable to XSS. The project mandates HttpOnly cookies for refresh tokens but sends access tokens in JSON. This is standard practice (short-lived, acceptable risk), but should be documented. |
| **Registration scope creep** | Medium | If self-registration is confirmed, email verification may be demanded next. Keep registration slice isolated and minimal. |
| **Controller vs Endpoint decision reversal** | Low | If team later prefers Minimal APIs, controller can be refactored to endpoint module without changing handlers. |

---

## Ready for Proposal

**Yes** — with the following prerequisites:

1. **Answer open product questions #1-4** (self-registration, password policy, account lockout, JWT algorithm).
2. **Confirm 3-slice chain** (or 4 if registration is in scope).
3. **Verify `.env` secrets strategy** with ops/DevOps (JWT secret rotation, key storage).

The orchestrator should launch `sdd-propose` once these questions are resolved.
