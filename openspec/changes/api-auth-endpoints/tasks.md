# Tasks: API Authentication Endpoints

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 800–1100 (all 3 slices) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | 3 force-chained PRs |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

## Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | JWT infra + UserSession + DI | PR #1 → tracker | NuGet, JwtTokenService, env, unit tests |
| 2 | Application CQRS handlers | PR #2 → PR #1 | Login/Refresh/Logout cmds + app tests |
| 3 | AuthController + integ tests | PR #3 → PR #2 | Controller, DTOs, cookies, e2e tests |

**Next apply**: Slice 1 only. Slices 2–3 planned below but deferred until Slice 1 is green.

## Slice 1 — JWT Infrastructure + UserSession (PR #1 → tracker `feature/api-auth-endpoints`)

- [x] 1.1 RED: Write `JwtTokenServiceTests` — issue+validate round-trip, tampered, expired
- [x] 1.2 GREEN: Add JwtBearer NuGet; impl `JwtTokenService : IJwtTokenService`, `JwtOptions` POCO
- [x] 1.3 RED: Write `UserSessionTests` — claim-to-property mapping from `ClaimsPrincipal`
- [x] 1.4 GREEN: Impl `UserSession : IUserSession` via `IHttpContextAccessor`; replace `NullUserSession` in DI
- [x] 1.5 RED: Write `TokenService` adapter tests (contract proof for `RevokeFamilyAsync`)
- [x] 1.6 GREEN: Impl `TokenService : ITokenService` adapter; wire in DI
- [x] 1.7 Add JwtBearer auth handler + options binding in `DependencyInjection.cs`
- [x] 1.8 Update `.env.example` — `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__RefreshTokenDays` (double-underscore convention for `Jwt:*` section binding)
- [x] 1.9 Verify: `dotnet test apps/api` — Unit (293 pass) and Application (67 pass) all green, no regressions. Historical Slice 1 note: IntegrationTests were blocked at that point by a pre-existing CS0433 `Program` type ambiguity, later resolved before final Slice 3 verification.

## Slice 2 — Application Auth Use Cases (PR #2 → PR #1)

- [x] 2.1 RED: Write `LoginCommandValidatorTests` + `LoginCommandHandlerTests` (success, invalid, blocked, weak pw → same 401)
- [x] 2.2 GREEN: Impl `LoginCommand`/`Handler`/`Validator` + `PasswordPolicy` + `TokenPairDto` + `ErrorCodes.Auth` + `IUserRepository.GetByEmailWithRolesAsync`
- [x] 2.3 RED: Write `RefreshTokenCommandHandlerTests` (rotation, expired, reuse→family revoke)
- [x] 2.4 GREEN: Impl `RefreshTokenCommand`/`Handler`/`Validator` with reuse signal catch
- [x] 2.5 RED: Write `LogoutCommandHandlerTests` (success, missing)
- [x] 2.6 GREEN: Impl `LogoutCommand`/`Handler`/`Validator`
- [x] 2.7 Verify: `dotnet test apps/api` — 293 UnitTests pass; 101 ApplicationTests pass (67 baseline + 18 Slice 2 + 16 remediation). Historical Slice 2 note: IntegrationTests compiled after the CS0433 `Program` ambiguity fix but were not yet executed until the later VPS host-Docker verification recorded in Slice 3.

## Slice 3 — API Auth Controller + Integration (PR #3 → PR #2)

- [x] 3.1 RED: Write `AuthEndpointsTests` — login 200+cookie, 401 generic, refresh rotation+reuse, logout 204
- [x] 3.2 GREEN: Impl `AuthController` (Login/Refresh/Logout) + `LoginRequest`/`TokenResponse` DTOs w/ explicit operators
- [x] 3.3 GREEN: Wire `Program.cs` — `AddControllers()`, `UseAuthentication()`, `UseAuthorization()`, cookie config
- [x] 3.4 RED: Write `AuthMiddlewareTests` — 401 on missing/invalid access token
- [x] 3.5 GREEN: Wire auth middleware pipeline; configure `Secure` policy (`SameAsRequest` dev, `Always` prod)
- [x] 3.6 Verify: `dotnet test apps/api` — unit + application tests pass (394 tests). Full IntegrationTests executed on the VPS through a Docker SDK container with the host Docker socket: 103 passed, 0 failed, 0 skipped.

### Slice 3 Remediation (2026-06-24)

8 review findings fixed:

| # | Severity | Finding | Resolution |
|---|----------|---------|------------|
| 1 | CRITICAL | Cookie path `/auth/refresh` breaks logout (browsers won't send cookie to `/auth/logout`) | Changed `Path` to `/auth` in `SetRefreshTokenCookie` and `ClearRefreshTokenCookie`; updated spec, design, proposal, and test assertions |
| 2 | CRITICAL | `Program.cs` missing `AddAuthorization()` before `UseAuthorization()` | Added `builder.Services.AddAuthorization()` in `Program.cs` |
| 3 | CRITICAL | Refresh rotation not actually asserted (value not compared) and no reuse-after-rotation test | Added `Assert.NotEqual(oldValue, newValue)` to `Refresh_ValidCookie_Returns200WithRotatedTokens`; added `Refresh_ReuseAfterRotation_Returns401AndClearsCookie` (login → rotate T1→T2 → present T1 → 401, cookie cleared, T2 also invalidated) |
| 4 | CRITICAL | `/auth/me` is production scope creep for middleware test probing | Removed `GET /auth/me` from production `AuthController`; created `TestAuthController` in integration test project; registered via `AuthWebApplicationFactory.AddApplicationPart()` |
| 5 | WARNING | Cookie clearing assertions weak (not verifying Max-Age=0, empty value, path consistency) | Added `AssertContainsClearSetCookie` helper: asserts `refreshToken=;`, `Max-Age=0`, and `path=/auth` in Set-Cookie header |
| 6 | WARNING | Integration tests use shared HttpClient (potential cookie-state dependency) | Each test method now creates its own `using var client = _fixture.CreateClient()` for fresh cookie jar per test |
| 7 | WARNING | Apply-progress/tasks overstate integration confidence | Artifacts now record final VPS runtime evidence: 293 Unit ✅ + 101 Application ✅ + 103 Integration ✅ = 497 tests passing. Integration tests execute on the VPS through a Docker SDK container with the host Docker socket. |
| 8 | WARNING | Test JWT secret hardcoded (not clearly named as non-secret test fixture constant) | Extracted `TestJwtSecret` const in `AuthWebApplicationFactory` with doc comment: "Non-secret test fixture constant — used ONLY for integration tests" |

## Rollback Notes

Each PR independently revertible:
- **PR #1 (Slice 1)**: revert DI, re-register `NullUserSession`, remove NuGet.
- **PR #2 (Slice 2)**: delete `Auth/` dir, revert `ErrorCodes` + repo method + `ICommandHandlerTResult.cs` + `TokenPairDto` changes. **DB rollback: run `Down` migration (`20260624120000_AddRefreshTokenUserId`) to drop `UserId` column from `refresh_tokens`** (Note: Slice 2 migration deletes all existing refresh_tokens in `Up()`, then adds `UserId NOT NULL`. `Down()` drops the column. No data is lost because refresh tokens are ephemeral.)
- **PR #3 (Slice 3)**: revert `Program.cs`, delete controller.
