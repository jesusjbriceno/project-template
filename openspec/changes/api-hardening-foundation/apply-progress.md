# Apply Progress: API Hardening Foundation — PR1 Remediation

**Date**: 2026-06-26
**Branch**: `feature/api-hardening-foundation-01-foundation`
**Strategy**: feature-branch-chain (PR 1)

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.5 | `apps/api/tests/Project.UnitTests/Middleware/ResultProblemDetailsMapperTests.cs` | Unit | ✅ 25/25 + 3/3 + 103/103 + 106/106 pre-existing | ✅ Written | ✅ Passed | ✅ 2 success guards + 500 + auth/detail/type cases | ✅ Shared helper + constants |
| 1.2 | `apps/api/tests/Project.UnitTests/Middleware/ApiExceptionHandlerTests.cs` | Unit | ✅ N/A (new) | ✅ Written | ✅ Passed | ✅ client-cancel, server-cancel, response-started | ✅ IProblemDetailsService wiring |
| 1.2 | `apps/api/tests/Project.IntegrationTests/Middleware/ExceptionHandlerIntegrationTests.cs` | Integration | ✅ 106/106 pre-existing | ✅ Updated | ✅ Passed | ✅ 500 detail/type assertions | ➖ None needed |

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Api.Controllers/Middleware/ApiExceptionHandler.cs` | Modified | Switched handler to `IProblemDetailsService`, added `Response.HasStarted` guard, client-cancellation suppression, and generic 500 ProblemDetails |
| `apps/api/src/Project.Api.Controllers/Middleware/ProblemDetailsResponseFactory.cs` | Created | Shared ProblemDetails builder with status URI type and safe 500 detail |
| `apps/api/src/Project.Api.Controllers/Middleware/ResultProblemDetailsMapper.cs` | Modified | Rejects successful results; uses shared ProblemDetails factory |
| `apps/api/src/Project.Api.Controllers/Middleware/ErrorCodeToHttpStatus.cs` | Modified | Replaced non-auth string literals with shared `ErrorCodes.General` constants |
| `apps/api/src/Project.Application/Common/ErrorCodes.cs` | Modified | Added shared non-auth error code constants |
| `apps/api/tests/Project.UnitTests/Middleware/ResultProblemDetailsMapperTests.cs` | Modified | Added success guards, safe 500 detail/type coverage, and constant-based assertions |
| `apps/api/tests/Project.UnitTests/Middleware/ApiExceptionHandlerTests.cs` | Created | Unit tests for cancellation, response-started guard, and safe 500 ProblemDetails |
| `apps/api/tests/Project.UnitTests/Middleware/ErrorCodeToHttpStatusTests.cs` | Modified | Switched non-auth mapping assertions to shared constants |
| `apps/api/tests/Project.IntegrationTests/Middleware/ExceptionHandlerIntegrationTests.cs` | Modified | Asserted safe 500 detail/type for exception handler |

## Verification Results

- `dotnet test apps/api --no-restore --verbosity minimal` ✅
- `dotnet build apps/api --no-restore` ✅
- Targeted: `dotnet test apps/api --no-restore --verbosity minimal --filter "FullyQualifiedName~ResultProblemDetailsMapperTests|FullyQualifiedName~ErrorCodeToHttpStatusTests|FullyQualifiedName~ExceptionHandlerIntegrationTests|FullyQualifiedName~ApiExceptionHandlerTests"` ✅

## Remaining Tasks

- [ ] PR2 auth controller contract updates
- [ ] PR3 XML docs / OpenAPI smoke docs
