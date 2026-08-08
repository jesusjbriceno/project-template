# Proposal: API Hardening Foundation Remediation

## Intent

Close the strict-verification blockers in `api-hardening-foundation` without expanding product scope. Framework-generated 404/405 responses must honor the public `extensions.code` contract, the OpenAPI dependency must be patched, and every previously uncovered scenario must have runtime evidence. This follows `AGENTS.md` Clean Architecture, TDD, OWASP, and `openspec/config.yaml` strict-TDD constraints.

## Proposal Question Round

Interactive scope is resolved: `ROUTING_NOT_FOUND` (404) and `METHOD_NOT_ALLOWED` (405) are stable public compatibility contracts. No further product decisions are open.

## Scope

### In Scope
- Replace bare status-code handling so routing 404 and method-mismatch 405 ProblemDetails include their stable public codes.
- Pin `Microsoft.OpenApi` explicitly to `2.7.5`.
- Add runtime coverage for all seven verification gaps: Result boundaries, enum serialization, XML schema description, smoke documentation, auth metadata, and deactivated-user login.
- Deliver one feature-branch-chain PR under 400 changed lines.

### Out of Scope
- New endpoints, auth behavior changes, OpenAPI exposure changes, or smoke-file workflow redesign.
- Product-code changes other than the status-code contract fix and package update.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `api-error-contract`: Framework 404/405 ProblemDetails MUST expose the stable codes.
- `api-documentation`: Runtime evidence MUST cover enum, XML schema, and smoke-document scenarios.
- `api-authentication`: Runtime evidence MUST cover login metadata and the deactivated-user generic-401 path.
- `application-layer`: Runtime evidence MUST cover Result no-throw and HTTP-agnostic-code scenarios.

## Approach

Use the existing API-layer `ProblemDetailsResponseFactory` from a custom status-code-pages delegate; preserve pipeline order and avoid Application HTTP dependencies. Add focused unit/integration/file-content tests, then rerun the full API suite and build.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `apps/api/src/Project.Api.Controllers/Program.cs` | Modified | Emit stable 404/405 error codes. |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Modified | Pin `Microsoft.OpenApi` 2.7.5. |
| `apps/api/tests/Project.*Tests/` | Modified/New | Cover seven missing scenarios and 404/405 codes. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Status handler conflicts with exception middleware | Low | Assert 404 and 405 bodies end-to-end. |
| Package compatibility regression | Low | Restore, build, and test with 2.7.5. |
| Review exceeds budget | Low | Keep one focused PR below 400 changed lines. |

## Rollback Plan

Revert the single remediation PR. This restores the prior status-page behavior and transitive package resolution; consumers must be notified because the two public codes are compatibility contracts once released.

## Dependencies

- Approved public codes: `ROUTING_NOT_FOUND` and `METHOD_NOT_ALLOWED`.
- `Microsoft.OpenApi` 2.7.5 availability.

## Success Criteria

- [ ] Framework 404/405 responses include the approved stable codes.
- [ ] Build has no NU1903 warning and the full API suite passes.
- [ ] All seven previously uncovered scenarios have passing runtime evidence.
- [ ] The remediation ships as one PR under 400 changed lines.
