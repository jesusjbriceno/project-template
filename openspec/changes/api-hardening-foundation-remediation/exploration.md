# Exploration: API Hardening Foundation Remediation

## Current State

The `api-hardening-foundation` change is fully implemented (15/15 tasks complete, 538/538 tests green) but **strict SDD verification failed** with verdict `fail`. The verify report identified 3 blockers and 3 critical findings:

1. **7 of 19 spec scenarios lack passing runtime evidence** — the suite is green but cannot prove these requirements.
2. **`UseStatusCodePages()` responses lack `extensions.code`** — framework-generated 404/405 ProblemDetails violate the all-error-response contract.
3. **NU1903 high-severity vulnerability** — transitive `Microsoft.OpenApi` 2.0.0 (from `Microsoft.AspNetCore.OpenApi` 10.0.10) has CVE-2026-49451 (circular schema reference DoS). Patched in 2.7.5.

All 15 tasks are checked complete. The code works. The gaps are in **runtime proof** and **one spec-violating implementation detail**.

## Affected Areas

| File | Why affected |
|------|-------------|
| `apps/api/src/Project.Api.Controllers/Program.cs` | `UseStatusCodePages()` (line 69) is bare — needs custom delegate to inject `extensions.code` into framework-generated ProblemDetails |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Needs explicit `Microsoft.OpenApi` >= 2.7.5 override to resolve NU1903 |
| `apps/api/tests/Project.IntegrationTests/Middleware/ExceptionHandlerIntegrationTests.cs` | Existing 404/405 tests must assert `extensions.code` after the fix |
| `apps/api/tests/Project.IntegrationTests/Auth/AuthEndpointsTests.cs` | Needs deactivated-user login test (partial scenario coverage) |
| `apps/api/tests/Project.IntegrationTests/Middleware/OpenApiGatingTests.cs` | Needs schema-description assertion for XML doc scenario |
| `apps/api/tests/Project.IntegrationTests/Middleware/EnumSerializationTests.cs` | New file — enum-as-string integration test |
| `apps/api/tests/Project.IntegrationTests/Middleware/SmokeFileTests.cs` | New file — validates `api-smoke.http` content |
| `apps/api/tests/Project.UnitTests/Controllers/AuthControllerMetadataTests.cs` | New file — reflection test for `[ProducesResponseType]` attributes |
| `apps/api/tests/Project.ApplicationTests/Common/ResultPatternBoundaryTests.cs` | New file — no-control-flow-exceptions + HTTP-agnostic codes |

## Verification Gap Inventory

### Gap 1: Seven Scenarios Without Runtime Coverage

| # | Requirement | Scenario | Current evidence | Fix approach |
|---|-------------|----------|-----------------|--------------|
| 1 | application-layer / Result Pattern | No control-flow exceptions | No test exercises a failed operation through a caller | Add unit test: call handler with invalid input, assert `Result.IsFailure` (no throw) |
| 2 | application-layer / Result Pattern | HTTP-agnostic error codes | No architectural boundary test | Add unit test: assert `ErrorCodes.Auth.*` constants contain no HTTP status substrings |
| 3 | api-documentation / Enum Serialization | Enum serialized as string | No endpoint returns an enum in tests | Add integration test: hit `/health` or similar, or add a test-only endpoint returning an enum DTO |
| 4 | api-documentation / XML Doc Generation | XML comments in OpenAPI schema | `OpenApiGatingTests` fetches doc but doesn't assert descriptions | Extend `OpenApiGatingTests` to assert `TokenResponse` schema has `description` from XML doc |
| 5 | api-documentation / .http Smoke Documentation | Smoke file covers auth endpoints | File exists but no test validates its content | Add file-content test: assert `api-smoke.http` contains required request patterns |
| 6 | api-authentication / Auth Response Metadata | Login action metadata complete | No reflection test for `[ProducesResponseType]` | Add unit test: reflect on `AuthController.Login`, assert attribute presence |
| 7 | api-authentication / Login Endpoint | Generic 401 — deactivated user | Only wrong-password and nonexistent-user tested | Add integration test: seed deactivated user, login, assert same 401 |

### Gap 2: `UseStatusCodePages()` Missing `extensions.code`

**Root cause**: `Program.cs` line 69 calls `app.UseStatusCodePages()` without a custom delegate. The default behavior (combined with `AddProblemDetails()`) writes a ProblemDetails body for status codes, but the `ProblemDetailsMiddleware` does NOT inject `extensions.code`. The spec requires ALL error responses to carry `extensions.code`.

**Fix**: Replace bare `UseStatusCodePages()` with `UseStatusCodePages(async context => { ... })` that uses `ProblemDetailsResponseFactory.Create()` to write a ProblemDetails response with the appropriate code (e.g., `"ROUTING_NOT_FOUND"` for 404, `"METHOD_NOT_ALLOWED"` for 405).

**Changed lines**: ~15 product code + ~10 test assertions = ~25 lines.

### Gap 3: NU1903 — `Microsoft.OpenApi` 2.0.0 Vulnerability

**Root cause**: `Microsoft.AspNetCore.OpenApi` 10.0.10 transitively depends on `Microsoft.OpenApi` 2.0.0, which has CVE-2026-49451 (high severity, CVSS 7.5 — circular schema reference DoS). Patched in `Microsoft.OpenApi` 2.7.5.

**Fix options**:
1. Add explicit `<PackageReference Include="Microsoft.OpenApi" Version="2.7.5" />` to override the transitive dependency.
2. Wait for a newer `Microsoft.AspNetCore.OpenApi` patch (10.0.10 is the latest available as of 2026-08-08).

**Recommendation**: Option 1 — explicit override. This is a standard NuGet practice for transitive vulnerability remediation.

**Changed lines**: ~2 lines in csproj.

## Approaches

### Approach 1: Single Remediation PR (Recommended)

All three gaps fixed in one PR targeting the feature tracker branch.

- **Pros**: Coherent review, single merge, all verification blockers resolved atomically, ~180 total lines well within 400-line budget.
- **Cons**: Larger diff than minimal, but still reviewable.
- **Effort**: Low — all changes are well-scoped and independent.
- **Estimated lines**: ~180 (25 product + 2 csproj + ~153 tests).

### Approach 2: Two chained PRs (A+C then B)

PR4a: Status code fix + NU1903 override (~27 lines). PR4b: Test coverage for 7 scenarios (~153 lines).

- **Pros**: Smaller individual diffs, product fix ships first.
- **Cons**: Extra PR overhead, two review cycles for what is fundamentally one remediation.
- **Effort**: Low-Medium.

### Approach 3: Three chained PRs (one per gap)

PR4a: Status code fix (~25 lines). PR4b: NU1903 override (~2 lines). PR4c: Test coverage (~153 lines).

- **Pros**: Maximum granularity.
- **Cons**: Excessive overhead for a remediation change. PR4b at 2 lines is not worth a standalone PR.
- **Effort**: Medium.

## Recommendation

**Approach 1: Single remediation PR.** Total ~180 lines is well within the 400-line review budget. The three gaps are logically one remediation effort (fixing the verification failure of `api-hardening-foundation`). A single PR keeps the review coherent and avoids unnecessary merge overhead.

If the user prefers chained PRs, Approach 2 (product fix + tests) is the only sensible split — PR4b at 2 lines is not a standalone PR.

## Remediation Task Sketch

### Phase 1: Product Code Fixes

- [ ] 1.1 Replace `app.UseStatusCodePages()` with custom delegate using `ProblemDetailsResponseFactory.Create()` — inject `extensions.code` for 404/405
- [ ] 1.2 Add explicit `Microsoft.OpenApi` 2.7.5 `PackageReference` to override transitive vulnerability
- [ ] 1.3 Update `ExceptionHandlerIntegrationTests` to assert `extensions.code` on 404/405 responses

### Phase 2: Runtime Coverage Tests

- [ ] 2.1 Add `Login_DeactivatedUser_Returns401SameMessage` integration test
- [ ] 2.2 Add `AuthControllerMetadata_HasCorrectProducesResponseTypeAttributes` unit test (reflection)
- [ ] 2.3 Add `EnumSerialization_ReturnsStringNotInteger` integration test
- [ ] 2.4 Extend `OpenApiGatingTests` to assert XML doc descriptions in schema
- [ ] 2.5 Add `SmokeFile_CoversAllAuthEndpoints` file-content test
- [ ] 2.6 Add `ResultPattern_NoControlFlowExceptions` application test
- [ ] 2.7 Add `ErrorCodes_AreHttpAgnostic` application test

### Phase 3: Verification

- [ ] 3.1 Run full test suite — assert 538 + 7 new = 545 passing
- [ ] 3.2 Run `dotnet build` — assert 0 warnings (NU1903 resolved)
- [ ] 3.3 Re-run SDD verification — assert verdict passes

## Risks

| Risk | Mitigation |
|------|-----------|
| `UseStatusCodePages` custom delegate may conflict with `ProblemDetailsMiddleware` ordering | Test both 404 and 405 paths; the delegate writes the body, `ProblemDetailsMiddleware` won't double-write if response has started |
| Explicit `Microsoft.OpenApi` 2.7.5 may have breaking changes vs 2.0.0 | The vulnerability is a DoS via circular refs; the API only generates (not parses) OpenAPI docs. Risk is minimal. Pin to 2.7.5 exactly if needed. |
| Deactivated-user test requires seeding a deactivated user in the test fixture | `AuthTestFixture` already seeds test users; add a deactivated user in `InitializeAsync` |
| Enum serialization test needs an endpoint that returns an enum | Use an existing endpoint or add a test-only endpoint; the health check doesn't return enums. May need a minimal test endpoint. |
| Smoke file test is a content assertion, not execution | This is acceptable — the spec says "covers auth endpoints", not "executes successfully". Content assertions prove coverage. |

## PR Split Decision

**PR4 should NOT be split.** Total estimated ~180 lines is well under the 400-line budget. The three gaps form a single remediation unit. Splitting adds merge overhead without meaningful review benefit.

If the review budget were tighter (e.g., 100 lines), then Approach 2 would be warranted.

## Ready for Proposal

**Yes.** The exploration is complete. The orchestrator should tell the user:

> "The remediation scope is well-defined: 3 verified gaps (7 untested scenarios, `extensions.code` missing on status-code pages, NU1903 vulnerability). Total ~180 lines, single PR recommended. Ready to proceed to `sdd-propose` for `api-hardening-foundation-remediation`."
