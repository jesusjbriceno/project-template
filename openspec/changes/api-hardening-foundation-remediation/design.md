# Design: API Hardening Foundation Remediation

## Technical Approach

Keep the remediation in the API composition/error boundary plus tests. Replace bare status pages with an API-layer writer that emits the existing `ProblemDetailsResponseFactory` contract for framework 404/405 responses, pin `Microsoft.OpenApi` 2.7.5 directly, and add the seven missing runtime proofs. Deliver one feature-branch-chain PR forecast at 330–390 authored changed lines, below the 400-line review budget.

## Architecture Decisions

| Decision | Alternatives / tradeoff | Choice and rationale |
|---|---|---|
| Status-page implementation | Inline delegate is shorter but hard to unit test; middleware duplicates framework behavior. | Add internal `FrameworkStatusCodePages.WriteAsync(StatusCodeContext)` and register it through `UseStatusCodePages`. This keeps `Program.cs` composition-only and enables RED tests. |
| Public code ownership | Adding framework codes to Application `ErrorCodes` introduces HTTP/routing knowledge. Literals invite drift. | API middleware owns `FrameworkErrorCodes.RoutingNotFound` and `.MethodNotAllowed`, valued `ROUTING_NOT_FOUND` and `METHOD_NOT_ALLOWED`. Names describe framework outcomes while preserving Clean Architecture. |
| Response writing | Direct JSON serialization can diverge from configured ProblemDetails behavior. | Resolve `IProblemDetailsService`, build via `ProblemDetailsResponseFactory`, and write once. Existing status/title/type/detail/`extensions.code` remains the public contract. |
| Dependency patch | Relying on the transitive graph leaves advisory resolution unstable. | Add an explicit `Microsoft.OpenApi` `2.7.5` reference beside `Microsoft.AspNetCore.OpenApi`; verify the resolved graph, restore, build, and NU1903 absence. |

## Data Flow

```text
Request -> exception/status pages -> auth -> routing/controllers
                    |
             downstream 404/405
                    v
      guard existing response -> map stable code
                    -> ProblemDetails service -> client
```

The delegate returns without writing when the response has started, has `ContentLength`, or has a non-empty `ContentType`. It handles only 404/405, never clears headers (preserving 405 `Allow`), and uses generic fixed details containing no path, route, exception, type, frame, or line data.

## File Changes

| File | Action | Description |
|---|---|---|
| `apps/api/src/Project.Api.Controllers/Program.cs` | Modify | Register the custom status-code-pages delegate without changing pipeline order. |
| `apps/api/src/Project.Api.Controllers/Middleware/FrameworkStatusCodePages.cs` | Create | Own API framework codes, guards, mapping, and ProblemDetails write. |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Modify | Pin `Microsoft.OpenApi` 2.7.5. |
| `apps/api/tests/Project.UnitTests/Middleware/FrameworkStatusCodePagesTests.cs` | Create | RED tests for 404/405 mapping and no-double-write behavior. |
| `apps/api/tests/Project.IntegrationTests/Middleware/ExceptionHandlerIntegrationTests.cs` | Modify | Assert complete 404/405 contract, generic details, no leakage, and 405 `Allow`. |
| `apps/api/tests/Project.ApplicationTests/Auth/LoginCommandHandlerTests.cs` | Modify | Prove bad credentials do not throw and return the expected failed Result. |
| `apps/api/tests/Project.ApplicationTests/Architecture/ApplicationBoundaryTests.cs` | Create | Prove no ASP.NET reference and no numeric HTTP suffix in error-code constants. |
| `apps/api/tests/Project.IntegrationTests/Documentation/ApiContractEvidenceTests.cs` | Create | Test-only enum endpoint plus enum, XML-schema-description, and `api-smoke.http` evidence. |
| `apps/api/tests/Project.IntegrationTests/Auth/AuthResponseContractTests.cs` | Create | Reflect attributes and compare login/refresh/logout OpenAPI responses. |
| `apps/api/tests/Project.IntegrationTests/Auth/AuthEndpointsTests.cs` | Modify | Seed an isolated deactivated user and assert generic 401 contract. |

No files are deleted.

## Interfaces / Contracts

`FrameworkStatusCodePages.WriteAsync(StatusCodeContext)` is internal. For 404/405 it preserves the status and headers and emits `application/problem+json` with `status`, standard `title`/`type`, generic `detail`, and stable `extensions.code`. Other statuses and pre-owned responses are unchanged.

## Testing Strategy

| Layer | Runtime proof |
|---|---|
| Unit | Writer mappings, stable constants, and sentinel-body no-double-write guard. |
| Application | Bad-credential Result/no-throw; assembly references and constant-shape boundary. |
| Integration | Full 404/405 contract; enum string round trip; XML schema summary; smoke-file commands; auth attributes/OpenAPI responses; isolated deactivated-user 401. |
| Verification | `dotnet restore apps/api`; `dotnet list ...Project.Api.Controllers.csproj package --include-transitive` resolves only `Microsoft.OpenApi 2.7.5`; `dotnet build apps/api` has no NU1903; `dotnet test apps/api` passes. |

## Threat Matrix

| Boundary | Applicability | Design response | Planned RED tests |
|---|---|---|---|
| Documentation-like paths | N/A — smoke content is read, never classified or executed. | None | None |
| Git repository selection | N/A — no Git invocation changes. | None | None |
| Commit state | N/A — no commit automation changes. | None | None |
| Push state | N/A — no push automation changes. | None | None |
| PR commands | N/A — delivery policy changes no command composition. | None | None |

Routing safety is covered separately by RED integration tests for unknown routes, method mismatch, preserved `Allow`, generic content, and no double write.

## Migration / Rollout

No data migration or feature flag is required. Apply with strict RED-GREEN-REFACTOR in one PR under 400 lines. The changed candidate receives a newly authorized RDD lineage after apply; the parent receipt remains audit history only. After both changes verify, archive the parent first and this successor second.

## Open Questions

None.
