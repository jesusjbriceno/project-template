# Apply Progress: API Authentication Endpoints — Slice 1

**Date**: 2026-06-23 (remediated 2026-06-23)
**Branch**: `feature/api-auth-endpoints-01-jwt-infrastructure`
**Strategy**: feature-branch-chain (PR #1 → tracker `feature/api-auth-endpoints`)

## TDD Cycle Evidence

### Original Slice 1 Implementation

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Project.UnitTests/Security/JwtTokenServiceTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 8/8 passed | ✅ 8 cases | ✅ Clean |
| 1.2 | — (impl) | — | N/A | — | ✅ NuGet + JwtTokenService | — | ✅ MapInboundClaims=false |
| 1.3 | `tests/Project.UnitTests/Security/UserSessionTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 7/7 passed | ✅ 7 cases | ✅ Clean |
| 1.4 | — (impl) | — | N/A | — | ✅ UserSession via IHttpContextAccessor | — | ✅ Role factory for superadmin |
| 1.5 | `tests/Project.UnitTests/Security/TokenServiceTests.cs` | Unit | N/A (new) | ✅ Written | ✅ 3/3 passed | ✅ 3 cases | ✅ Clean |
| 1.6 | — (impl) | — | N/A | — | ✅ TokenService adapter | — | ➖ None needed |
| 1.7 | — (DI) | — | N/A | — | ✅ DI wiring complete | — | ➖ Structural |
| 1.8 | — (env) | — | N/A | — | ✅ .env.example updated | — | ➖ Structural |
| 1.9 | — (verify) | Unit + App | ✅ 287+67 pre-existing | — | ✅ No regressions | — | ➖ None needed |

### Review Remediation

| Finding | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---------|-----------|-------|------------|-----|-------|-------------|----------|
| F1 (expired token) | `JwtTokenServiceTests.cs` | Unit | ✅ 287+67 | ✅ CS1729 (missing ctor) | ✅ 10/10 passed | ✅ 1 case | ✅ IClock extracted |
| F3 (UserSession auth) | `UserSessionTests.cs` | Unit | ✅ 287+67 | ✅ 2 tests FAILED | ✅ 9/9 passed | ✅ 2 cases | ✅ Clean |
| F4 (refresh hash) | `JwtTokenServiceTests.cs` | Unit | ✅ 287+67 | ✅ 1 test FAILED | ✅ 10/10 passed | ✅ 1 case | ✅ Clean |
| F5 (env vars) | — (docs) | — | — | — | ✅ Jwt__Secret convention | — | ➖ Docs-only |
| F6 (counts) | — (docs) | — | — | — | ✅ Accurate 291+67=358 | — | ➖ Docs-only |

### Second Remediation (2026-06-23) — Remaining Review Warnings

| Finding | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|---------|-----------|-------|------------|-----|-------|-------------|----------|
| F7a (.env placeholder) | — (ops) | — | — | — | ✅ `CHANGE_ME` (8 chars, fails `[MinLength(32)]`) | — | ➖ Ops-only |
| F7b (artifact env names) | — (docs) | — | — | — | ✅ All `JWT_*` → `Jwt__*` in spec, design, apply-progress | — | ➖ Docs-only |
| F7c (issuer/audience tests) | `JwtTokenServiceTests.cs` | Unit | ✅ 291+67 | ✅ Tests written (exercises existing validation) | ✅ 12/12 passed (2 new) | ✅ 2 cases | ✅ No code changes needed |

### Test Summary
- **Total tests written (Slice 1 original)**: 18 (8 JwtTokenService + 7 UserSession + 3 TokenService)
- **Tests added in remediation**: 6 (1 expired token + 2 UserSession auth + 1 refresh hash + 2 issuer/audience)
- **Total tests written**: 24
- **Total tests passing**: 360 (293 Unit + 67 Application)
- **Layers used**: Unit (24)
- **Approval tests** (refactoring): None — all new code
- **Pure functions created**: 2 (`Base64UrlEncode`, `ComputeSha256Hash`)

## Files Changed

### Original Slice 1

| File | Action | What Was Done |
|------|--------|---------------|
| `src/Project.Application/Abstractions/Security/IJwtTokenService.cs` | Created | Application-layer JWT contract: `GenerateAccessToken`, `GenerateRefreshToken`, `ValidateAccessToken` |
| `src/Project.Infrastructure/Security/JwtOptions.cs` | Created | Configuration POCO with `[Required]`, `[MinLength(32)]`, `[Range]` validation |
| `src/Project.Infrastructure/Security/JwtTokenService.cs` | Created | HS256 implementation: issue+validate access tokens, generate refresh tokens |
| `src/Project.Infrastructure/Security/UserSession.cs` | Created | Real `IUserSession` via `IHttpContextAccessor`; extracts identity from JWT principal |
| `src/Project.Infrastructure/Security/TokenService.cs` | Created | Thin adapter: `ITokenService` → `IRefreshTokenRepository.RevokeFamilyAsync` |
| `src/Project.Infrastructure/Security/NullUserSession.cs` | Deleted | Replaced by `UserSession` |
| `src/Project.Infrastructure/DependencyInjection.cs` | Modified | Replaced `NullUserSession` → `UserSession`; added `IHttpContextAccessor`, `ITokenService`; new `AddJwtAuthentication()` |
| `src/Project.Infrastructure/Project.Infrastructure.csproj` | Modified | Added `Microsoft.AspNetCore.Authentication.JwtBearer` v10.0.9 |
| `.env.example` | Modified | Added `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshTokenDays` |
| `tests/Project.UnitTests/Security/JwtTokenServiceTests.cs` | Created | 8 tests: round-trip, tampered, wrong key, invalid format, empty, role claims, refresh token uniqueness |
| `tests/Project.UnitTests/Security/UserSessionTests.cs` | Created | 7 tests: authenticated user mapping, unauthenticated, missing sub, invalid GUID sub, empty roles |
| `tests/Project.UnitTests/Security/TokenServiceTests.cs` | Created | 3 tests: family ID propagation, cancellation token flow, exception propagation |
| `openspec/changes/api-auth-endpoints/tasks.md` | Modified | Tasks 1.1–1.9 marked [x] |

### Review Remediation

| File | Action | What Was Done |
|------|--------|---------------|
| `src/Project.Infrastructure/Security/JwtTokenService.cs` | Modified | Added `IClock` dependency; `GenerateAccessToken` uses `_clock.UtcNow` for `exp`/`iat`; `ValidateAccessToken` uses custom `LifetimeValidator` against `_clock`; `ComputeSha256Hash` now hashes the raw string (not raw bytes) so lookup matches client-presented token |
| `src/Project.Infrastructure/Security/UserSession.cs` | Modified | `IsAuthenticated` now requires `Identity?.IsAuthenticated == true` AND parseable `sub`; `Email`/`Roles`/`UserId` guard with `IsAuthenticated` first — identity data not exposed for unauthenticated principals |
| `src/Project.Infrastructure/DependencyInjection.cs` | Modified | `JwtTokenService` registration now passes `IClock` from DI container |
| `.env.example` | Modified | Renamed `JWT_*` → `Jwt__*` (double-underscore convention mapping to `Jwt:*` config section); placeholder secret is now obviously invalid |
| `tests/Project.UnitTests/Security/JwtTokenServiceTests.cs` | Modified | +1 test: `ValidateAccessToken_ExpiredToken_ReturnsNull` (deterministic via `MutableClock`); +1 test: `GenerateRefreshToken_HashIsSha256OfRawString` (proves hash corresponds to raw); added `MutableClock` fake; all instantiations pass `FixedClock` |
| `tests/Project.UnitTests/Security/UserSessionTests.cs` | Modified | +2 tests: `PrincipalWithValidSub_ButIdentityNotAuthenticated_IsNotAuthenticated`, `UnauthenticatedPrincipal_DoesNotExposeEmailOrRoles` |
| `openspec/changes/api-auth-endpoints/tasks.md` | Modified | Task 1.9 verification wording updated: truthful about IntegrationTests CS0433 block |
| `openspec/changes/api-auth-endpoints/apply-progress.md` | Modified | Corrected test counts (358, not 372); added remediation evidence |

### Second Remediation (2026-06-23)

| File | Action | What Was Done |
|------|--------|---------------|
| `.env.example` | Modified | `Jwt__Secret` placeholder shortened to `CHANGE_ME` (8 chars — fails `[MinLength(32)]` at startup) |
| `openspec/changes/api-auth-endpoints/specs/api-authentication/spec.md` | Modified | All env var references use .NET double-underscore names: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshTokenDays` |
| `openspec/changes/api-auth-endpoints/design.md` | Modified | All env var references updated to double-underscore convention |
| `openspec/changes/api-auth-endpoints/apply-progress.md` | Modified | Historical record line 56 corrected; test counts updated to 293+67=360 |
| `tests/Project.UnitTests/Security/JwtTokenServiceTests.cs` | Modified | +2 tests: `ValidateAccessToken_WrongIssuer_ReturnsNull`, `ValidateAccessToken_WrongAudience_ReturnsNull` (12 total JwtTokenService tests) |

## Verification Results

### Original Verification

| Test Project | Pass | Fail | Skip | Status |
|-------------|------|------|------|--------|
| `Project.UnitTests` | 287 | 0 | 0 | ✅ All pass (includes 18 new) |
| `Project.ApplicationTests` | 67 | 0 | 0 | ✅ No regressions |
| `Project.IntegrationTests` | N/A | — | — | ⚠️ Pre-existing CS0433 `Program` ambiguity (not caused by this change) |

### Post-Remediation Verification (2026-06-23)

| Test Project | Pass | Fail | Skip | Status |
|-------------|------|------|------|--------|
| `Project.UnitTests` | 291 | 0 | 0 | ✅ All pass (+4 new remediation tests) |
| `Project.ApplicationTests` | 67 | 0 | 0 | ✅ No regressions |
| `Project.IntegrationTests` | N/A | — | — | ⚠️ Still blocked by pre-existing CS0433 `Program` ambiguity |

### Second Remediation Verification (2026-06-23)

| Test Project | Pass | Fail | Skip | Status |
|-------------|------|------|------|--------|
| `Project.UnitTests` | 293 | 0 | 0 | ✅ All pass (+2 issuer/audience tests; 293 total) |
| `Project.ApplicationTests` | 67 | 0 | 0 | ✅ No regressions |
| `Project.IntegrationTests` | N/A | — | — | ⚠️ Still blocked by pre-existing CS0433 `Program` ambiguity |

**Test runner**: Docker `mcr.microsoft.com/dotnet/sdk:10.0` (dotnet not available on host)

## Deviations from Design

1. **`MapInboundClaims = false`**: Set on `JwtSecurityTokenHandler` to preserve original JWT claim types (`sub`, `email`) instead of mapping to .NET `ClaimTypes`. This avoids claim-type confusion during round-trip and keeps the `ClaimsPrincipal` predictable.

2. **Role objects from claims**: `UserSession.Roles` creates ephemeral `Role` entities via `Role.Create()` for session context. The "superadmin" role is marked `IsSystem = true` so Domain-layer guard checks (`IsSuperadminRole()`) work correctly.

3. **`NullUserSession.cs` deleted**: Removed per design's infrastructure delete list.

4. **`IClock` injected into `JwtTokenService`** (remediation): Added `IClock` dependency to enable deterministic expiry testing and align with the project's existing clock abstraction pattern. The design originally had `JwtTokenService` using `DateTime.UtcNow` directly. This is a quality improvement, not a behavior change.

5. **Custom `LifetimeValidator`** (remediation): Replaces default `ValidateLifetime = true` with a custom delegate that checks `exp` against the injected `IClock`. Same security semantics, fully testable.

6. **`ComputeSha256Hash` hashes raw string** (remediation): Previously hashed the raw random bytes; now hashes the base64url-encoded raw token string so that client-presented tokens can be looked up by hash without a decode step.

## Issues Found

1. **Integration test compilation failure (pre-existing)** (still present): `Project.IntegrationTests` has a CS0433 `Program` type ambiguity between `Project.Api.Controllers` and `Project.MigrationService`. This predates Slice 1 and must be resolved before Slice 3.

2. **Docker-only test execution**: Host lacks `dotnet` SDK. All test runs used `docker run mcr.microsoft.com/dotnet/sdk:10.0`. Verification in CI/CD or a dev machine with dotnet installed is recommended before merge.

3. **apply-progress originally double-counted tests** (fixed): Previous apply-progress reported 372 = 287 Unit + 67 App + 18 new, but 287 already included the 18 new. Correct count is 358 = 291 Unit + 67 App.

4. **`.env.example` originally used flat `JWT_*` vars** (fixed): Renamed to `Jwt__*` to match the .NET double-underscore convention for `configuration.GetSection("Jwt")` binding.

5. **`.env.example` placeholder was valid** (fixed): Original `CHANGE_ME__this_must_be_at_least_32_characters!` was 48 chars and passed `[MinLength(32)]`. Replaced with `CHANGE_ME` (8 chars) so unchanged template config fails fast at startup.

6. **SDD artifacts referenced flat env names** (fixed): `spec.md`, `design.md`, and `apply-progress.md` all updated to use `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshTokenDays`.

7. **No issuer/audience validation tests** (fixed): Added `ValidateAccessToken_WrongIssuer_ReturnsNull` and `ValidateAccessToken_WrongAudience_ReturnsNull` to `JwtTokenServiceTests.cs`. Both pass — the existing `TokenValidationParameters` already enforced issuer/audience validation; these tests close a coverage gap.

## Remaining Tasks (Slices 2–3)

- [ ] 2.1–2.7: Application auth use cases (Login/Refresh/Logout handlers, validators, ErrorCodes.Auth, PasswordPolicy)
- [ ] 3.1–3.6: AuthController + DTOs + integration tests + Program.cs wiring

## Workload / PR Boundary

- **Mode**: feature-branch-chain, Slice 1 of 3
- **Current branch**: `feature/api-auth-endpoints-01-jwt-infrastructure`
- **Target**: tracker branch `feature/api-auth-endpoints`
- **Boundary**: Tasks 1.1–1.9 complete + review findings remediated. Build stays green with 293 Unit + 67 Application tests.
- **Estimated review budget**: ~350 changed lines (within 400-line budget)
- **Second remediation**: 3 review warnings fixed (env placeholder, artifact names, issuer/audience tests). ~20 lines added net.

## Status

**9/29 tasks complete. Slice 1 done + all review warnings remediated — Ready for next batch (Slice 2: Application auth use cases).**
