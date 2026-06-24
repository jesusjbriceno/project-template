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
- [x] 1.9 Verify: `dotnet test apps/api` — Unit (293 pass) and Application (67 pass) all green, no regressions; IntegrationTests blocked by pre-existing CS0433 `Program` type ambiguity (not caused by this change). Full `dotnet test apps/api` cannot run until CS0433 is resolved.

## Slice 2 — Application Auth Use Cases (PR #2 → PR #1)

- [x] 2.1 RED: Write `LoginCommandValidatorTests` + `LoginCommandHandlerTests` (success, invalid, blocked, weak pw → same 401)
- [x] 2.2 GREEN: Impl `LoginCommand`/`Handler`/`Validator` + `PasswordPolicy` + `TokenPairDto` + `ErrorCodes.Auth` + `IUserRepository.GetByEmailWithRolesAsync`
- [x] 2.3 RED: Write `RefreshTokenCommandHandlerTests` (rotation, expired, reuse→family revoke)
- [x] 2.4 GREEN: Impl `RefreshTokenCommand`/`Handler`/`Validator` with reuse signal catch
- [x] 2.5 RED: Write `LogoutCommandHandlerTests` (success, missing)
- [x] 2.6 GREEN: Impl `LogoutCommand`/`Handler`/`Validator`
- [x] 2.7 Verify: `dotnet test apps/api` — 293 UnitTests pass; 101 ApplicationTests pass (67 baseline + 18 Slice 2 + 16 remediation). IntegrationTests now compile after the CS0433 `Program` ambiguity fix; execution is blocked in this Docker SDK environment by Docker-in-Docker/Testcontainers constraints.

## Slice 3 — API Auth Controller + Integration (PR #3 → PR #2)

- [ ] 3.1 RED: Write `AuthEndpointsTests` — login 200+cookie, 401 generic, refresh rotation+reuse, logout 204
- [ ] 3.2 GREEN: Impl `AuthController` (Login/Refresh/Logout) + `LoginRequest`/`TokenResponse` DTOs w/ explicit operators
- [ ] 3.3 GREEN: Wire `Program.cs` — `AddControllers()`, `UseAuthentication()`, `UseAuthorization()`, cookie config
- [ ] 3.4 RED: Write `AuthMiddlewareTests` — 401 on missing/invalid access token
- [ ] 3.5 GREEN: Wire auth middleware pipeline; configure `Secure` policy (`SameAsRequest` dev, `Always` prod)
- [ ] 3.6 Verify: `dotnet test apps/api` — all integration tests pass

## Rollback Notes

Each PR independently revertible:
- **PR #1 (Slice 1)**: revert DI, re-register `NullUserSession`, remove NuGet.
- **PR #2 (Slice 2)**: delete `Auth/` dir, revert `ErrorCodes` + repo method + `ICommandHandlerTResult.cs` + `TokenPairDto` changes. **DB rollback: run `Down` migration (`20260624120000_AddRefreshTokenUserId`) to drop `UserId` column from `refresh_tokens`** (Note: Slice 2 migration deletes all existing refresh_tokens in `Up()`, then adds `UserId NOT NULL`. `Down()` drops the column. No data is lost because refresh tokens are ephemeral.)
- **PR #3 (Slice 3)**: revert `Program.cs`, delete controller.
