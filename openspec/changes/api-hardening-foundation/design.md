# Design: API Hardening Foundation

## Technical Approach

Implement the hardening in `Project.Api.Controllers` only: RFC 7807 `ProblemDetails`, a DI-backed exception handler, and a Result-to-HTTP mapper. Put `UseExceptionHandler()` and `UseStatusCodePages()` **before** auth middleware in `Program.cs` so framework/auth failures and empty 404/405 bodies normalize consistently. Keep Application HTTP-agnostic. Add built-in OpenAPI in Development, enable XML docs for API types, and ship a repo-root `api-smoke.http` with `@baseUrl` variants. No Swashbuckle.

Cancellation exceptions (`OperationCanceledException` / `TaskCanceledException`) are treated as expected request-abort paths and must not become noisy 500 telemetry.

## Architecture Decisions

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 1 | Pipeline order | `UseExceptionHandler()` + `UseStatusCodePages()` before `UseAuthentication()` / `UseAuthorization()` | Captures auth/pipeline exceptions and framework-generated empty statuses uniformly. |
| 2 | Exception testing | Use a test-only throwing controller/endpoint via integration host | Clear end-to-end path; avoids brittle manual `HttpContext` stubs. |
| 3 | Mapper contract | Return `IActionResult` / `ActionResult`, not `ObjectResult`-only APIs | Keeps the contract honest for non-`ObjectResult` outcomes. |
| 4 | Helper visibility | Keep `ErrorCodeToHttpStatus` internal and expose it to test assemblies with `InternalsVisibleTo` | Clean testability without making the helper public. |
| 5 | Status mapping table | Active codes only; `AUTH_USER_BLOCKED` and `AUTH_TOKEN_REVOKED` are reserved/future-proof | Preserves no-enumeration behavior and avoids claiming emitted codes that do not exist yet. |
| 6 | Cancellation handling | Ignore request-aborted cancellations in the handler/logging path | Prevents false 500 telemetry for client disconnects/timeouts. |
| 7 | OpenAPI auth scheme | Deferred in this slice; document built-in OpenAPI availability first | Stays aligned with .NET built-ins without expanding scope. |
| 8 | XML docs | Enable docs in the API project and document `public partial class Program` instead of broad CS1591 suppression | Keeps analyzers loud and fixes the public-entry-point warning explicitly. |
| 9 | Enum converter | `JsonStringEnumConverter` belongs to the foundation runtime slice (`Program.cs`) | Makes the serialization choice explicit so it is not dropped from later docs-only work. |
| 10 | `.http` smoke file | Repo-root `api-smoke.http` with `@baseUrl` and commented Docker Compose / `dotnet run` alternatives | One smoke artifact that works in both container and local workflows. |

## Data Flow

`Request -> Exception/Status middleware -> Auth middleware -> Controller -> Result mapper -> ProblemDetails`

`ResultProblemDetailsMapper` is a pure API-layer helper; `ApiExceptionHandler` writes a generic 500 body and never exposes exception text.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `apps/api/src/Project.Api.Controllers/Program.cs` | Modify | Register ProblemDetails/exception handling, place middleware before auth, add `JsonStringEnumConverter`, gate OpenAPI to Development, and add the XML summary for `public partial class Program`. |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Modify | Enable XML documentation generation. |
| `apps/api/src/Project.Api.Controllers/Middleware/ApiExceptionHandler.cs` | Create | Safe 500 handler with cancellation-aware behavior. |
| `apps/api/src/Project.Api.Controllers/Middleware/ResultProblemDetailsMapper.cs` | Create | `IActionResult`/`ActionResult` mapping for failures. |
| `apps/api/src/Project.Api.Controllers/Middleware/ErrorCodeToHttpStatus.cs` | Create | Internal status-code table exposed to tests via `InternalsVisibleTo`, plus reserved future auth codes. |
| `apps/api/src/Project.Api.Controllers/Controllers/AuthController.cs` | Modify | Use the mapper and add response metadata. |
| `apps/api/tests/Project.IntegrationTests/*Throw*.cs` | Create | Test-only throwing endpoint/controller for exception-handler coverage. |
| `apps/api/tests/Project.IntegrationTests/Middleware/*` | Create/Modify | Assert mapper, cancellation, and 404/500 integration paths. |
| `api-smoke.http` | Create | Base URL smoke requests with local/container alternatives. |

## Testing Strategy

| Layer | What | How |
|-------|------|-----|
| Unit | Active code mapping and safe detail text | Table-driven tests against the helper. |
| Integration | 404/405 and thrown exception behavior | Use the test-only throwing endpoint/controller in the hosted test app. |
| Integration | Auth contract stays ProblemDetails-compatible | Update auth endpoint assertions without changing logout missing-cookie semantics. |
| Smoke | Manual API flow | `api-smoke.http` against either Docker Compose or local `dotnet run`. |

## PR Slices

| Slice | Scope |
|-------|-------|
| PR1 | `Program.cs`, middleware, mapper, enum converter, core tests |
| PR2 | Auth controller metadata + auth contract updates |
| PR3 | XML docs, `public partial class Program` summary, `api-smoke.http`, OpenAPI doc notes |
