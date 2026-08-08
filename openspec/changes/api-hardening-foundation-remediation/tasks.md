# Tasks: API Hardening Foundation Remediation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Total tasks | 16 |
| Phase breakdown | Phase 1: 2, Phase 2: 7, Phase 3: 3, Phase 4: 4 |
| Estimated changed lines | 330-390 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: feature-branch-chain
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Focused test command | Runtime harness | Rollback boundary |
|------|------|-----------|----------------------|-----------------|-------------------|
| 1 | 404/405 stable codes + OpenApi 2.7.5 pin + 7 runtime proofs | PR 1 | `dotnet test apps/api` | `dotnet run` then `curl /nonexistent` (expect `ROUTING_NOT_FOUND`) and `curl /openapi/v1.json` (expect descriptions) | `FrameworkStatusCodePages.cs`, `Program.cs` delegate registration, csproj pin, all new/modified test files — all independently revertable |

Single PR remains safe: 10 files, 330-390 estimated authored lines, no migration, no behavior change beyond the two stable public codes. Feature-branch-chain is the configured strategy but no child split is needed at this budget.

## Phase 1: Foundation

- [x] 1.1 Pin `Microsoft.OpenApi` to `2.7.5` in `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` alongside existing `Microsoft.AspNetCore.OpenApi`.
- [x] 1.2 Create `apps/api/src/Project.Api.Controllers/Middleware/FrameworkStatusCodePages.cs` — `internal static class` with `FrameworkErrorCodes` constants (`RoutingNotFound` = `ROUTING_NOT_FOUND`, `MethodNotAllowed` = `METHOD_NOT_ALLOWED`) and `WriteAsync(StatusCodeContext)` stub.

## Phase 2: RED Tests

- [x] 2.1 Create `apps/api/tests/Project.UnitTests/Middleware/FrameworkStatusCodePagesTests.cs` — RED: 404 maps to `ROUTING_NOT_FOUND`, 405 maps to `METHOD_NOT_ALLOWED`, sentinel-body guard skips write when response already owned.
- [x] 2.2 Modify `apps/api/tests/Project.IntegrationTests/Middleware/ExceptionHandlerIntegrationTests.cs` — RED: full 404/405 contract (ProblemDetails body, stable `extensions.code`, generic detail, no route/path/leakage, 405 `Allow` header preserved).
- [x] 2.3 Create `apps/api/tests/Project.ApplicationTests/Architecture/ApplicationBoundaryTests.cs` — RED: Application assembly has no `Microsoft.AspNetCore.*` reference; `ErrorCodes` constants hold no numeric HTTP status suffix.
- [x] 2.4 Modify `apps/api/tests/Project.ApplicationTests/Auth/LoginCommandHandlerTests.cs` — RED: bad-credential handler invocation does not throw, returns `Result` failure with expected error code.
- [x] 2.5 Modify `apps/api/tests/Project.IntegrationTests/Auth/AuthEndpointsTests.cs` — RED: seed isolated deactivated user, POST login returns 401 with `extensions.code == "AUTH_INVALID_CREDENTIALS"` and generic detail.
- [x] 2.6 Create `apps/api/tests/Project.IntegrationTests/Documentation/ApiContractEvidenceTests.cs` — RED: enum endpoint returns string name in raw JSON and round-trips; `/openapi/v1.json` DTO schema `description` is non-empty; `api-smoke.http` contains `login`, `refresh`, `logout`, `@baseUrl`, Docker Compose and `dotnet run` variants.
- [x] 2.7 Create `apps/api/tests/Project.IntegrationTests/Auth/AuthResponseContractTests.cs` — RED: reflect `[ProducesResponseType]` on `AuthController` (login 200/400/401, refresh 200/400/401, logout 204/400); parse `/openapi/v1.json` and confirm matching response types.

## Phase 3: GREEN

- [x] 3.1 Implement `FrameworkStatusCodePages.WriteAsync` — resolve `IProblemDetailsService`, guard pre-owned response (started / ContentLength / ContentType), map 404/405 to stable codes via `ProblemDetailsResponseFactory`, write once with generic fixed detail.
- [x] 3.2 Register status-code-pages delegate in `apps/api/src/Project.Api.Controllers/Program.cs` without changing pipeline order.
- [x] 3.3 Run `dotnet test apps/api` — all Phase 2 RED tests now green.

## Phase 4: Verification

- [x] 4.1 `dotnet restore apps/api` — no NU1903 warning.
- [x] 4.2 `dotnet list apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj package --include-transitive` — only `Microsoft.OpenApi/2.7.5`.
- [x] 4.3 `dotnet build apps/api` — zero warnings, zero errors.
- [x] 4.4 `dotnet test apps/api` — full suite green, all 7 new scenarios passing.
