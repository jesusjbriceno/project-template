# Apply Progress: API Hardening Foundation — PR1 Foundation + PR2 Auth Integration

**Date**: 2026-06-26
**Branch PR1**: `feature/api-hardening-foundation-01-foundation`
**Branch PR2**: `feature/api-hardening-foundation-02-auth-contract` (targets PR1 branch)
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

## TDD Cycle Evidence — Phase 2

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 2.1 | `AuthEndpointsTests.cs` | Integration | ✅ 9/9 | ✅ Written (6 tests) | N/A | N/A | N/A |
| 2.2 | `AuthEndpointsTests.cs` | Integration | N/A | N/A | ✅ 9/9 | ✅ 6 error codes + 3 success paths | ➖ None needed |
| 2.3 | `AuthController.cs` | N/A (metadata) | N/A | N/A | ✅ 9/9 | N/A | N/A |
| 2.4 | `AuthEndpointsTests.cs` | Integration | N/A | N/A | ✅ 9/9 | N/A | ✅ Removed ErrorResponseContract |

### Test Summary
- **Safety Net**: 9/9 AuthEndpointsTests passing before changes
- **RED phase**: 6/6 error-path tests failed (controller returned ad-hoc objects)
- **GREEN phase**: 9/9 passing after controller refactored
- **Full suite**: UnitTests 324/324 ✅, ApplicationTests 103/103 ✅, Auth+Middleware Integration 12/12 ✅

## PR2 Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Api.Controllers/Controllers/AuthController.cs` | Modified | Replaced 6 ad-hoc error-body sites with `ResultProblemDetailsMapper.Map()`, added `[ProducesResponseType]` on all 3 actions |
| `apps/api/tests/Project.IntegrationTests/Auth/AuthEndpointsTests.cs` | Modified | Changed 6 error-path tests to `ProblemDetails` assertions, removed `ErrorResponseContract` |
| `openspec/changes/api-hardening-foundation/tasks.md` | Modified | Marked Phase 2 tasks 2.1-2.4 complete [x] |

## Verification Results

- `dotnet test apps/api/tests/Project.IntegrationTests --filter "AuthEndpointsTests"` ✅ 9/9
- `dotnet test apps/api/tests/Project.UnitTests` ✅ 324/324
- `dotnet test apps/api/tests/Project.ApplicationTests` ✅ 103/103
- `dotnet build apps/api --no-restore` ✅ 0 warnings, 0 errors

## Remaining Tasks

- [ ] PR3: XML docs, OpenAPI gating, `api-smoke.http`
