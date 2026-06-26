# Exploration: API Hardening Foundation

## Current State

The auth endpoints (`POST /auth/login`, `/auth/refresh`, `/auth/logout`) are complete and archived with 497 tests green. The API surface works but lacks production-grade foundations:

| Concern | Current State | Gap |
|---------|--------------|-----|
| **Error responses** | Ad-hoc anonymous objects `{ code, message }` returned directly from controller actions | No RFC 7807 ProblemDetails, no global exception handler, no consistent error shape |
| **OpenAPI** | Zero wiring in `Program.cs`. No `/openapi/v1.json`, no Swagger UI, no `[ProducesResponseType]` attributes | API consumers have no machine-readable contract |
| **.http smoke docs** | No `.http` files exist anywhere in the repo | No living documentation or quick manual smoke test harness |
| **Rate limiting** | No `AddRateLimiter()`, no policies, no `[EnableRateLimiting]` | `/auth/login` is unprotected against brute-force |
| **Global error handling** | No `IExceptionHandler`, no `UseExceptionHandler()`, no `AddProblemDetails()` in `Program.cs` | Unhandled exceptions return the ASP.NET Core default HTML/text developer page (or blank 500) |

### Program.cs — Current Pipeline (53 lines)

```
builder.Services.AddHealthChecks()
builder.Services.AddControllers()
builder.Services.AddInfrastructure(connectionString)
builder.Services.AddJwtAuthentication(config)
builder.Services.AddAuthorization()

// ❌ No: AddProblemDetails(), AddExceptionHandler<T>()
// ❌ No: AddOpenApi(), ConfigureHttpJsonOptions()
// ❌ No: AddRateLimiter()

app.UseAuthentication()
app.UseAuthorization()
app.MapHealthChecks("/health")
app.MapControllers()

// ❌ No: UseExceptionHandler(), UseStatusCodePages()
// ❌ No: MapOpenApi()
```

### AuthController Error Pattern (current)

```csharp
// Login failure — ad-hoc anonymous object
return Unauthorized(new { code = ErrorCodes.Auth.InvalidCredentials, message = "Invalid credentials." });

// Refresh failure — same pattern, different status
return BadRequest(new { code = ErrorCodes.Auth.RefreshTokenMissing, message = "Refresh token is required." });

// Refresh token reuse — same pattern
return Unauthorized(new { code = result.Error.Code, message = result.Error.Message });
```

Integration tests deserialize these into a test-local `ErrorResponseContract(string Code, string Message)`. Any change to the error shape requires updating these inline test contracts.

### AuthController Success Pattern (current)

```csharp
return Ok(response);                    // Login / Refresh
return NoContent();                      // Logout
```

No `[ProducesResponseType]` attributes exist on any action. The only XML doc comments are `<summary>` on the class and methods — no response type documentation.

## Affected Areas

| File | Why Affected |
|------|-------------|
| `apps/api/src/Project.Api.Controllers/Program.cs` | Add ProblemDetails middleware, OpenAPI, `JsonStringEnumConverter`; rate limiter (if included) |
| `apps/api/src/Project.Api.Controllers/Controllers/AuthController.cs` | Add `[ProducesResponseType]` attributes; update error returns to use ProblemDetails-compatible pattern |
| `apps/api/src/Project.Api.Controllers/Contracts/LoginRequest.cs` | Already has `<summary>` XML doc — verify completeness for OpenAPI |
| `apps/api/src/Project.Api.Controllers/Contracts/TokenResponse.cs` | Already has `<summary>` XML doc — verify completeness for OpenAPI |
| `apps/api/src/Project.Api.Controllers/Project.Api.Controllers.csproj` | Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` for XML doc → OpenAPI flow |
| `apps/api/src/Project.Api.Controllers/Middleware/` (NEW) | `ApiExceptionHandler.cs` implementing `IExceptionHandler`; `ResultExtensions.cs` for `Result` → `ProblemDetails` mapping |
| `apps/api/src/Project.Application/Common/ErrorCodes.cs` | Potentially add HTTP status code metadata or keep mapping in API layer |
| `api-smoke.http` (NEW at repo root) | Smoke test file covering all auth endpoints with success and error paths |
| `apps/api/tests/Project.IntegrationTests/Auth/AuthEndpointsTests.cs` | Update `ErrorResponseContract` to match ProblemDetails shape OR add new ProblemDetails-specific assertions |
| `apps/api/tests/Project.IntegrationTests/Auth/AuthMiddlewareTests.cs` | Test that unauthenticated requests return ProblemDetails 401 |
| `.env.example` | Add rate limit configuration keys (if included) |

## Approaches

### 1. ProblemDetails + Result Mapping

**Approach**: Introduce RFC 7807 ProblemDetails via ASP.NET Core's built-in middleware (`AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`). Create an `IExceptionHandler` that maps Application `Error.Code` → HTTP status code. Add extension methods on `Result`/`Result<T>` to produce `ProblemDetails` responses.

**Mapping strategy**: Keep the `Error.Code` → HTTP status code mapping in the API layer (not in Application). This preserves the Application layer's independence from HTTP concerns.

| Error Code Pattern | HTTP Status | ProblemDetails Title |
|-------------------|-------------|---------------------|
| `AUTH_INVALID_CREDENTIALS` | 401 | Unauthorized |
| `AUTH_USER_BLOCKED` | 401 | Unauthorized |
| `AUTH_TOKEN_EXPIRED` | 401 | Unauthorized |
| `AUTH_TOKEN_REVOKED` | 401 | Unauthorized |
| `AUTH_TOKEN_REUSE_DETECTED` | 401 | Unauthorized |
| `AUTH_REFRESH_TOKEN_MISSING` | 400 | Bad Request |
| `NOT_FOUND` (future) | 404 | Not Found |
| `VALIDATION_ERROR` (future) | 400 | Bad Request |
| `CONFLICT` (future) | 409 | Conflict |
| Unhandled / unknown | 500 | Internal Server Error |

**AuthController changes**: Replace anonymous `new { code, message }` with `Problem()` or extension methods that produce `ProblemDetails`. Benefit: consistent error shape STARTING NOW, before more endpoints are added.

**Middleware/ directory**: Create `Middleware/ApiExceptionHandler.cs` implementing `IExceptionHandler`. This is the single place that catches unhandled exceptions and maps them to safe ProblemDetails responses (OWASP: never leak internal details).

- Pros: RFC 7807 compliance, consistent error shape for all future endpoints, single exception handler for global coverage, no extra NuGet packages (ASP.NET Core built-in), `.http` test files gain structured error assertions
- Cons: Requires updating ALL integration test error assertions (tests use `ErrorResponseContract { Code, Message }` — shape changes to `{ type, title, status, detail }`), must ensure generic messages for auth (no info leakage per OWASP), controller error returns need coordinated update
- Effort: Medium

### 2. OpenAPI Metadata for Auth

**Approach**: Add built-in .NET 10 OpenAPI support (`AddOpenApi()` + `MapOpenApi()` in dev mode). Add `[ProducesResponseType]` attributes to every auth controller action. Add `JsonStringEnumConverter` for enum serialization (future-proofing). Enable XML doc generation in `.csproj` so existing `<summary>` comments flow into the OpenAPI schema automatically.

**No Swashbuckle**: .NET 10 uses built-in `Microsoft.AspNetCore.OpenApi`. Do NOT add any `Swashbuckle.*` NuGet package.

**OpenAPI doc at `/openapi/v1.json`** — standard .NET 9+ convention.

- Pros: Zero NuGet dependencies, machine-readable API contract for frontend and external consumers, XML doc comments auto-flow into schema, `.http` test authoring benefits from known schemas, future endpoints automatically included
- Cons: `[ProducesResponseType]` attributes add ~15-20 lines of annotations per controller action (but these are one-time, declarative, and self-documenting), must ensure auth error responses are documented (400, 401)
- Effort: Low

### 3. `.http` Smoke Docs

**Approach**: Create `api-smoke.http` at repo root with requests for every auth endpoint (success + error paths). Use `@baseUrl = http://localhost:5032` matching `launchSettings.json`. Document login, refresh, logout, invalid credentials, missing cookies, and reuse detection scenarios.

- Pros: Living documentation, quick manual smoke test after any change, no infrastructure needed (VS Code / Rider support `.http` files natively), zero code dependencies
- Cons: Requires the API to be running to execute, cookie-based auth requires manual extraction of `Set-Cookie` values in `.http` files (not automatic like browser), must be maintained as endpoints evolve
- Effort: Low

### 4. Auth Rate Limiting

**Approach**: Add `AddRateLimiter()` with a fixed-window policy for `/auth/login` (e.g., 5 requests per minute per IP). Apply via `[EnableRateLimiting("auth-login")]` on the login action. Return 429 Too Many Requests with ProblemDetails body. Make limits configurable via `appsettings.json`.

**Policy design**:
- `/auth/login`: 5 req/min/IP (brute-force protection)
- `/auth/refresh`: 30 req/min/IP (normal usage, brief burst)
- `/auth/logout`: 30 req/min/IP (normal usage)

**Configuration**: `appsettings.json` section `RateLimiting:Auth` with `LoginPermitLimit`, `LoginWindowSeconds`, `RefreshPermitLimit`, etc.

**Testing challenge**: Rate limit integration tests are inherently timing-dependent. A fixed-window policy with a short window (e.g., 1 second) and high permit count works for test determinism but doesn't match production values. Solution: use `IConfiguration` overrides in `AuthWebApplicationFactory` to set permissive test values, and write tests that verify the `Retry-After` header is present rather than exact hit counts.

- Pros: OWASP A07 (Identification and Authentication Failures) mitigation, .NET 10 built-in `System.Threading.RateLimiting` (no extra packages), configurable per-endpoint, ProblemDetails 429 responses are well-defined
- Cons: Tests are non-deterministic (timing-dependent), policy tuning requires ops knowledge (what limits?), adds configuration surface area, conceptually separate from API documentation/standards concerns (different risk profile), can be its own focused SDD change with dedicated security review
- Effort: Medium

## Recommendation

### Include in this change

1. **ProblemDetails + Result Mapping** (`sdd/api-hardening-foundation`)
2. **OpenAPI Metadata for Auth** (`sdd/api-hardening-foundation`)
3. **`.http` Smoke Docs** (`sdd/api-hardening-foundation`)

### Defer to a dedicated security-hardening change

4. **Auth Rate Limiting** → future change `api-auth-rate-limiting`

**Rationale for including #1-#3 together**: These three items are tightly coupled around "API consumer contract." ProblemDetails defines the error response shape, OpenAPI documents it, and `.http` files exercise it. Implementing them as one orchestrated change ensures consistency — the OpenAPI spec describes the exact ProblemDetails shape that the exception handler produces, and the `.http` smoke tests validate both.

**Rationale for deferring rate limiting**: Auth rate limiting is a **security control** with a different risk profile, different testing strategy (non-deterministic timing), different configuration surface area, and different stakeholder review needs (ops/SRE should weigh in on limits). It also depends on ProblemDetails for proper 429 responses, making it a natural **next** change rather than a parallel one. Keeping it separate allows focused security review without diluting the API contract work.

### Suggested PR Slicing (400-line budget, force-chained)

**PR Slice A: ProblemDetails + Exception Handler** (~250-350 changed lines)
- `Middleware/ApiExceptionHandler.cs` — `IExceptionHandler` mapping `Error.Code` → HTTP status
- `Middleware/ResultExtensions.cs` — extension methods for `Result` → `ProblemDetails`
- `Program.cs` — `AddProblemDetails()`, `AddExceptionHandler<ApiExceptionHandler>()`, `UseExceptionHandler()`, `UseStatusCodePages()`
- Update `AuthController.cs` error returns to use ProblemDetails pattern
- Integration tests: update `ErrorResponseContract` → `ProblemDetailsContract` assertions
- Tests: ~100-150 lines

**PR Slice B: OpenAPI + .http Docs** (~200-300 changed lines)
- `Program.cs` — `AddOpenApi()`, `MapOpenApi()` (dev-only), `JsonStringEnumConverter`
- `AuthController.cs` — `[ProducesResponseType]` attributes on all 3 actions
- `Project.Api.Controllers.csproj` — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `api-smoke.http` — smoke test file at repo root (~60-80 lines)
- Integration tests: verify OpenAPI doc endpoint returns valid JSON (optional, lightweight)
- Tests: ~50-80 lines

**Combined total**: ~450-650 lines across 2 PRs. Each PR independently under 400 lines.

**Dependency**: Slice B depends on Slice A — the `[ProducesResponseType(StatusCodes.Status401Unauthorized)]` attribute must match the ProblemDetails shape produced by the exception handler.

### What This Change Does NOT Include

| Deferred item | Rationale | Future change |
|---------------|-----------|---------------|
| Auth rate limiting | Security control with different risk/review profile; depends on ProblemDetails for 429 | `api-auth-rate-limiting` |
| CORS configuration | No frontend origin defined yet | `api-cors-config` (when web client exists) |
| Audit/security logging table | Requires dedicated `SecurityEvent` entity + migration | `api-audit-logging` |
| Response compression | No large payloads exist yet | Deferred indefinitely |
| Account lockout | Requires User model changes + attempt tracking | `api-account-lockout` |
| `ActionResult<T>` migration for AuthController | Breaking change to integration tests; no runtime benefit yet | Optional future cleanup |
| Health endpoint OpenAPI metadata | Trivial, can be added any time | Inline with OpenAPI setup |

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Integration test breakage** | High | Medium | Error response shape changes from `{ code, message }` to RFC 7807 `{ type, title, status, detail }`. All `AuthEndpointsTests` assertions and inline test contracts must be updated. This is deliberate — the new shape is what consumers should expect. |
| **Auth error info leakage** | Medium | High | ProblemDetails `detail` field must use generic messages for auth failures (e.g., "Invalid credentials"), never "User not found" or "Password incorrect". OWASP: prevent user enumeration. Must explicitly configure the exception handler to use generic messages for AUTH_* codes. |
| **OpenAPI spec exposes internal types** | Low | Medium | If `JsonStringEnumConverter` or XML doc generation inadvertently exposes enum values or type names, audit generated spec. Mitigation: review `/openapi/v1.json` output in CI. |
| **Missing `[ProducesResponseType]` on error paths** | Medium | Low | Auth controller has 4+ error paths per action (400, 401). Missing one creates an incomplete spec. Mitigation: code review checklist + integration test that asserts expected response types exist in OpenAPI doc. |
| **`MapOpenApi()` in production** | Low | Medium | Should only be available in Development. Mitigation: wrap in `if (app.Environment.IsDevelopment())` block. |
| **`.http` file port mismatch** | Low | Low | `launchSettings.json` uses port 5032; Docker Compose uses 8080. `.http` file should document both with comments. |

## Ready for Proposal

**Yes** — with the following decisions confirmed:

1. **Scope**: ProblemDetails + OpenAPI + `.http` docs in this change. Rate limiting deferred to `api-auth-rate-limiting`.
2. **Slicing**: 2 chained PRs (ProblemDetails first, OpenAPI second). Each under 400 lines.
3. **Controller mutation**: AuthController error returns change from anonymous objects to ProblemDetails. Integration test contracts update accordingly.
4. **No new NuGet packages** — all infrastructure is ASP.NET Core built-in (.NET 10).
5. **OWASP A07 constraint**: Generic error messages for all auth failures — no user enumeration.

The orchestrator should launch `sdd-propose` once the scope is confirmed.

---

## Recommended Scope

| Item | Decision | Reasoning |
|------|----------|-----------|
| ProblemDetails + Result mapping | **Include** | Foundational for all future endpoints. RFC 7807 compliance. |
| OpenAPI metadata for auth | **Include** | Critical for API consumers. Zero NuGet deps (.NET 10 built-in). |
| `.http` smoke docs | **Include** | Trivial effort (~60 lines), pairs naturally with OpenAPI. |
| Auth rate limiting | **Defer** | Different risk profile, non-deterministic tests, separate ops concern. Natural next change. |

## Review Workload Initial Take

| Slice | Scope | Est. lines | Budget |
|-------|-------|-----------|--------|
| A — ProblemDetails + exception handler | Middleware, controller error returns, Program.cs wiring, test contract updates | 250-350 | ✅ ≤400 |
| B — OpenAPI + .http docs | Attributes, csproj XML docs, Program.cs OpenAPI, smoke file, lightweight test | 200-300 | ✅ ≤400 |
| **Combined** | | **450-650** | ⚠️ Over >400 — requires chaining |

**Chained PRs recommended**: Yes (2 PRs)
**400-line budget risk**: Low (each slice independently under budget)
**Decision needed before apply**: No (slicing strategy is clear; rate limiting deferred by design)
