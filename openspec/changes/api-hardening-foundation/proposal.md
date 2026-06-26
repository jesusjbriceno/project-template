# Proposal: API Hardening Foundation

## Intent

Establish a production-grade API consumer contract before adding more endpoints: RFC 7807 errors, configurable OpenAPI exposure, and living `.http` smoke docs for the existing auth flow. Constraints: AGENTS.md Clean Architecture/TDD/OWASP rules and `openspec/config.yaml` strict TDD apply.

## Scope

### In Scope
- Map Application `Result.Failure` outcomes to RFC 7807 `ProblemDetails`, preserving machine-readable codes in `extensions.code`.
- Add global safe exception/status-code handling and auth-safe generic error details.
- Add configurable OpenAPI availability and auth response metadata.
- Add repo-root `.http` smoke docs using `@baseUrl` and auth-flow examples.

### Out of Scope
- Auth rate limiting; deferred to `api-auth-rate-limiting`.
- CORS, audit logging, account lockout, response compression.
- Product implementation in this phase.

## Capabilities

### New Capabilities
- `api-error-contract`: RFC 7807 ProblemDetails, Result-to-HTTP mapping, safe exception/status-code responses, and `extensions.code` contract.
- `api-documentation`: Configurable OpenAPI exposure and `.http` smoke documentation conventions.

### Modified Capabilities
- `api-authentication`: Auth endpoints document and return ProblemDetails-compatible error responses while preserving existing auth lifecycle behavior.
- `application-layer`: Result pattern remains HTTP-agnostic; API layer owns Result-to-ProblemDetails mapping.

## Approach

Use ASP.NET Core built-ins (`AddProblemDetails`, `IExceptionHandler`, status-code handling, `AddOpenApi`/`MapOpenApi`) with no Swashbuckle. Keep HTTP mapping in API layer. Expose auth-safe codes where appropriate but keep user-facing auth details generic to avoid enumeration.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `apps/api/src/Project.Api.Controllers/Program.cs` | Modified | Wire ProblemDetails, exception/status handling, configurable OpenAPI. |
| `apps/api/src/Project.Api.Controllers/Controllers/AuthController.cs` | Modified | Replace ad-hoc errors and add response metadata. |
| `apps/api/src/Project.Api.Controllers/Middleware/` | New | Exception handler and Result mapping helpers. |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Modified | Enable XML docs if needed for OpenAPI. |
| `apps/api/tests/Project.IntegrationTests/Auth/` | Modified | Assert ProblemDetails contract. |
| `api-smoke.http` | New | Auth smoke documentation with `@baseUrl`. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Error-shape test breakage | High | TDD update contracts to ProblemDetails deliberately. |
| Auth information leakage | Medium | Generic `detail`; specific code only when safe. |
| OpenAPI exposed unintentionally | Low | Configuration-gated availability. |

## Rollback Plan

Revert the chained PR slice: first OpenAPI/`.http`, then ProblemDetails mapping. Existing auth behavior returns to prior ad-hoc error responses.

## Dependencies

- ASP.NET Core .NET 10 built-ins only; no new NuGet packages.
- Future `api-auth-rate-limiting` depends on this for 429 ProblemDetails.

## Success Criteria

- [ ] Auth failures return RFC 7807 with `extensions.code` and no sensitive details.
- [ ] OpenAPI availability is configurable and documents auth responses.
- [ ] `.http` smoke docs use `@baseUrl` and cover the auth flow.
- [ ] Work remains sliceable into chained PRs under 400 changed lines each.
