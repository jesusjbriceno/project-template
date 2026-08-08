```yaml
schema: gentle-ai.verify-result/v1
evidence_revision: sha256:38dda6309ddb8d46dd3eac29f45d4855787d915116879ebd86f908e1ddc60835
verdict: fail
blockers: 1
critical_findings: 1
requirements: 14/14
scenarios: 20/20
test_command: dotnet test apps/api --no-restore --verbosity minimal
test_exit_code: 0
test_output_hash: sha256:6342bfbf923f114ba9459d971e73d0bf28dcd437f555823c5c5ddebf401380d4
build_command: dotnet build apps/api --no-restore --verbosity minimal
build_exit_code: 0
build_output_hash: sha256:95a69ee153c224760f4d3582ff0990a4042855dac1a7404156a108a4c235edcb
```

## Verification Report

**Change**: `api-hardening-foundation`
**Mode**: Strict TDD; hybrid (OpenSpec + Engram)
**Review lineage**: `review-api-hardening-parent-remediation-20260808`
**Verdict**: **FAIL**

### Completeness
| Metric | Value |
|---|---:|
| Parent tasks total | 15 |
| Parent tasks checked complete | 15 |
| Parent tasks unchecked | 0 |
| Actual requirements runtime-verified | 14 / 14 |
| Actual scenarios with passing covering runtime tests | 20 / 20 |
| Verification product-code changes | 0 |

All specification requirements and scenarios now have passing runtime coverage. The count is 20 scenarios, not the stale 19 recorded by the previous parent report.

### Build & Tests Execution
| Command | Exit | Result | Output hash |
|---|---:|---|---|
| `dotnet test apps/api --no-restore --verbosity minimal` | 0 | 556 passed, 0 failed, 0 skipped: 106 Application, 334 Unit, 116 Docker-backed Integration | `sha256:6342bfbf923f114ba9459d971e73d0bf28dcd437f555823c5c5ddebf401380d4` |
| `dotnet build apps/api --no-restore --verbosity minimal` | 0 | 0 warnings, 0 errors | `sha256:95a69ee153c224760f4d3582ff0990a4042855dac1a7404156a108a4c235edcb` |

### Spec Compliance Matrix
| Requirement | Scenario coverage | Passing runtime evidence | Result |
|---|---:|---|---|
| application-layer / Result Pattern | 3 / 3 | `ResultTests`; `LoginCommandHandlerTests.Handle_BadCredentials_DoesNotThrow_ReturnsFailureResult`; `ApplicationBoundaryTests` | COMPLIANT |
| api-error-contract / ProblemDetails Error Shape | 2 / 2 | `AuthEndpointsTests.Login_InvalidCredentials_Returns401Generic`; `ExceptionHandlerIntegrationTests.UnhandledException_Returns500ProblemDetailsSafeDetail` | COMPLIANT |
| api-error-contract / Cancellation Is Not A 500 Error | 1 / 1 | `ApiExceptionHandlerTests.TryHandleAsync_ClientCancellation_ReturnsTrueAndSuppressesProblemDetails` | COMPLIANT |
| api-error-contract / Result-to-HTTP Mapping | 1 / 1 | `ResultProblemDetailsMapperTests`; `ErrorCodeToHttpStatusTests` | COMPLIANT |
| api-error-contract / Global Exception Handler | 1 / 1 | `ExceptionHandlerIntegrationTests.UnhandledException_Returns500ProblemDetailsSafeDetail` | COMPLIANT |
| api-error-contract / Status Code Pages | 1 / 1 | `ExceptionHandlerIntegrationTests.NotFoundRoute_Returns404ProblemDetails`, `MethodNotAllowed_Returns405ProblemDetails`; `FrameworkStatusCodePagesTests` | COMPLIANT |
| api-documentation / Configurable OpenAPI Availability | 2 / 2 | `OpenApiGatingTests` Development and Production cases | COMPLIANT |
| api-documentation / Enum Serialization | 1 / 1 | `ApiContractEvidenceTests.EnumEndpoint_ReturnsStringName_NotInteger` | COMPLIANT |
| api-documentation / XML Doc Generation | 1 / 1 | `ApiContractEvidenceTests.OpenApiSchema_TokenResponse_HasNonEmptyDescription` | COMPLIANT |
| api-documentation / .http Smoke Documentation | 1 / 1 | `ApiContractEvidenceTests.SmokeHttpFile_ContainsAuthEndpointsAndBaseUrlVariants` | COMPLIANT |
| api-authentication / Auth Response Metadata | 1 / 1 | `AuthResponseContractTests` reflection and OpenAPI assertions | COMPLIANT |
| api-authentication / Login Endpoint | 2 / 2 | Existing valid-login coverage; `AuthEndpointsTests.Login_DeactivatedUser_Returns401SameGenericResponse` | COMPLIANT |
| api-authentication / Refresh Endpoint | 2 / 2 | `AuthEndpointsTests.Refresh_ValidCookie_Returns200WithRotatedTokens`; `Refresh_ReuseAfterRotation_Returns401AndClearsCookie` | COMPLIANT |
| api-authentication / Logout Endpoint | 1 / 1 | `AuthEndpointsTests.Logout_ValidCookie_Returns204AndClearsCookie` | COMPLIANT |

### Correctness and Design Coherence
| Decision | Status | Evidence |
|---|---|---|
| API owns HTTP mapping | COMPLIANT | Application-boundary tests prove no ASP.NET Core reference or HTTP-status semantics. |
| Safe errors, cancellation, and framework statuses | COMPLIANT | Unit and integration tests prove generic 500, aborted-request suppression, 404/405 ProblemDetails, stable codes, and body preservation. |
| Development-only built-in OpenAPI | COMPLIANT | Development and Production integration tests pass. |
| Documentation contract | COMPLIANT | Runtime OpenAPI schema and repository smoke-document tests pass. |

### Successor Evidence and History Separation
The successor change `api-hardening-foundation-remediation` supplies the runtime evidence that closes the prior parent verification gaps. Its admitted report has evidence revision `sha256:cd95ff2905080c9514016534f80149952ba98ededb504edad7fe62b2765a3051`, 7/7 successor requirements, 20/20 successor scenarios, and a 556-test clean run. That evidence is referenced here only to establish final behavioral compliance of the parent specification. The parent does not claim the successor's PR4 implementation or tests as parent-authored code.

### TDD Compliance
| Check | Result | Details |
|---|---|---|
| Parent TDD evidence reported | PARTIAL | `apply-progress.md` records TDD rows only for parent tasks 3.1-3.5. |
| Parent task-level RED/GREEN traceability | INCOMPLETE | Tasks 1.1-2.4 have no retained parent TDD-cycle rows. |
| Current GREEN confirmation | PASS | The full Docker-backed suite passed: 556/556. |
| Successor remediation TDD evidence | PASS | The successor report records 16/16 task rows and passing runtime coverage. |

**TDD Compliance**: The current behavior is fully proven, but the parent’s Strict-TDD process record is incomplete and cannot be retroactively asserted from successor evidence.

### Test Layer Distribution
| Layer | Tests | Files | Tool |
|---|---:|---:|---|
| Unit | 6 | 1 | xUnit |
| Application | 3 | 2 | xUnit |
| Integration | 10 | 4 | xUnit + WebApplicationFactory + Docker/Testcontainers |
| E2E | 0 | 0 | Not available |

### Assertion Quality
**Assertion quality**: No tautologies, ghost loops, assertion-free tests, smoke-only assertions, or incomplete scenario assertions were found in the successor evidence files inspected.

### Issues Found
**CRITICAL**
1. Strict TDD is active, but the parent `apply-progress.md` retains TDD-cycle evidence for only 5 of 15 parent tasks. The successor’s 16/16 task record proves its own remediation work; it cannot truthfully reconstruct the missing RED/GREEN history for parent tasks 1.1-2.4.

**WARNING**
1. None for product behavior, requirements, scenarios, build, or current test execution.

### Verdict
**FAIL** — all 14 requirements and all 20 scenarios have passing runtime evidence, and the current Docker-backed test/build run is clean. Final PASS is not truthful under Strict TDD because the parent lacks required task-level TDD process evidence. The approved successor lineage closes behavioral evidence only; it does not make PR4 code parent-authored or reconstruct missing parent TDD history.
