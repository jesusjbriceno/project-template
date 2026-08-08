# Tasks: API Hardening Foundation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~500 (across 3 PRs, each < 400) |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Exception handler, mapper, status table, Program.cs wiring, core tests | PR 1 | base = feature/tracker branch |
| 2 | Auth controller ProblemDetails + response metadata, auth test updates | PR 2 | base = PR 1 branch |
| 3 | XML docs, OpenAPI gating, .http smoke file | PR 3 | base = PR 2 branch |

## Phase 1: Foundation — Exception Handling & Mapping

- [x] 1.1 [T] Create test-only throwing controller in IntegrationTests for handler coverage
- [x] 1.2 [RED→GREEN] Integration test (unhandled exception → 500 ProblemDetails) + implement `ApiExceptionHandler.cs`
- [x] 1.3 [RED→GREEN] Integration test (404/405 → ProblemDetails) + wire `UseExceptionHandler()` / `UseStatusCodePages()` before auth in Program.cs
- [x] 1.4 [T] Create `ErrorCodeToHttpStatus.cs` — internal table, `InternalsVisibleTo`, reserved auth codes
- [x] 1.5 [RED→GREEN] Unit tests + implement `ResultProblemDetailsMapper.cs` — IActionResult mapping with `extensions.code`
- [x] 1.6 Configure `JsonStringEnumConverter` in Program.cs (both controller and minimal API options)

## Phase 2: Auth Integration

- [x] 2.1 [RED] Update `AuthEndpointsTests` — assert ProblemDetails shape on all error paths
- [x] 2.2 [GREEN] Modify `AuthController` — replace ad-hoc `{ code, message }` with mapper-based ProblemDetails
- [x] 2.3 Add `[ProducesResponseType]` metadata on login/refresh (200, 400, 401) and logout (204, 400)
- [x] 2.4 [REFACTOR] Remove test `ErrorResponseContract`; deserialize as `ProblemDetails` and check `extensions.code`

## Phase 3: Documentation & OpenAPI

- [x] 3.1 Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `Project.Api.Controllers.csproj`
- [x] 3.2 Add XML doc summary on `public partial class Program` in Program.cs
- [x] 3.3 Gate OpenAPI: register `AddOpenApi()` + `MapOpenApi()` only in Development
- [x] 3.4 Create `api-smoke.http` — auth flow covering login (success/401), refresh (success/missing-cookie), logout (success/missing-cookie 400), with Docker/local `@baseUrl` variants
- [x] 3.5 Build & verify — `dotnet build` zero warnings, OpenAPI loads in dev, 404 in prod
