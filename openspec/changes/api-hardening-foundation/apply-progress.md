# Apply Progress: API Hardening Foundation — PR1 + PR2 + PR3 Complete

**Date**: 2026-08-08
**Branch PR1**: `feature/api-hardening-foundation-01-foundation`
**Branch PR2**: `feature/api-hardening-foundation-02-auth-contract` (targets PR1 branch)
**Branch PR3**: `feature/api-hardening-foundation-03-docs-openapi` (targets PR2 branch)
**Strategy**: feature-branch-chain

## Phase 1 — Completed (PR1)

- [x] 1.1 Test-only throwing controller
- [x] 1.2 `ApiExceptionHandler.cs` — safe 500, cancellation-aware
- [x] 1.3 404/405 → ProblemDetails, middleware wired before auth
- [x] 1.4 `ErrorCodeToHttpStatus.cs` — internal lookup table
- [x] 1.5 `ResultProblemDetailsMapper.cs` — IActionResult mapping
- [x] 1.6 `JsonStringEnumConverter` configured

## Phase 2 — Completed (PR2)

- [x] 2.1 [RED] Updated `AuthEndpointsTests` — 6 error-path tests now assert `ProblemDetails` (Status, Title, `extensions.code`)
- [x] 2.2 [GREEN] Modified `AuthController` — replaced ad-hoc `{ code, message }` with `ResultProblemDetailsMapper.Map()`
- [x] 2.3 Added `[ProducesResponseType]` metadata on Login/Refresh/Logout
- [x] 2.4 [REFACTOR] Removed `ErrorResponseContract` from tests; all error assertions use `ProblemDetails`

## Phase 3 — Completed (PR3)

- [x] 3.1 Enabled `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in csproj
- [x] 3.2 Added XML doc summary on `public partial class Program` + fixed all CS1591 warnings
- [x] 3.3 Gated OpenAPI: `AddOpenApi()` + `MapOpenApi()` only in Development; added `Microsoft.AspNetCore.OpenApi` package
- [x] 3.4 Created `api-smoke.http` — auth flow with Docker/local `@baseUrl` variants
- [x] 3.5 Build & verify — 0 code warnings, 327 unit tests passing, OpenAPI 2/2 integration tests passing

## TDD Cycle Evidence — Phase 3

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 3.1 | N/A | Structural | N/A | N/A | N/A | ➖ Config only | N/A |
| 3.2 | N/A | Structural | N/A | N/A | N/A | ➖ Comment only | N/A |
| 3.3 | `OpenApiGatingTests.cs` | Integration | ✅ 327/327 unit | ✅ Written (2 tests) | ✅ 2/2 passed | ✅ Dev+Prod scenarios | ➖ None needed |
| 3.4 | N/A | Documentation | N/A | N/A | N/A | N/A | N/A |
| 3.5 | N/A | Verification | N/A | N/A | ✅ Build 0 warnings | N/A | N/A |

### Test Summary (PR3)
- **Safety Net**: 327/327 unit tests passing before changes
- **RED phase**: Dev test failed (Expected OK, Actual NotFound); Prod test passed (endpoint absent)
- **GREEN phase**: 2/2 OpenAPI gating tests passing after `AddOpenApi()`/`MapOpenApi()` added
- **Full build**: 0 code warnings, 0 errors (only NU1903 advisory for transitive dep)
- **Changed lines**: 211 (33 modified + 52 api-smoke.http + 126 test file)

## PR3 Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Api.Controllers/Program.cs` | Modified | Added `AddOpenApi()` + `MapOpenApi()` gated by `IsDevelopment()`, XML doc on `public partial class Program` |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Modified | Added `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, added `Microsoft.AspNetCore.OpenApi` package |
| `apps/api/src/Project.Api.Controllers/Controllers/AuthController.cs` | Modified | Added XML doc on constructor (fix CS1591) |
| `apps/api/src/Project.Api.Controllers/Contracts/LoginRequest.cs` | Modified | Added XML doc on explicit operator (fix CS1591) |
| `apps/api/src/Project.Api.Controllers/Contracts/TokenResponse.cs` | Modified | Added XML doc on explicit operator (fix CS1591) |
| `apps/api/tests/Project.IntegrationTests/Middleware/OpenApiGatingTests.cs` | Created | Integration tests for OpenAPI gating (Dev=200, Prod=404) |
| `api-smoke.http` | Created | Auth flow smoke documentation with Docker/local `@baseUrl` variants |

## Remaining Tasks

None — all 15 tasks complete across PR1, PR2, PR3.
