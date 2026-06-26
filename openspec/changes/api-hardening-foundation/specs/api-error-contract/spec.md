# api-error-contract Specification

## Purpose
RFC 7807 ProblemDetails error shape, Result-to-HTTP mapping, global exception handling, and `extensions.code` machine-readable contract.

## Requirements

### Requirement: ProblemDetails Error Shape
All API error responses MUST be RFC 7807 `ProblemDetails` with `type`, `title`, `status`, `detail`, and `extensions.code`. `detail` MUST NOT leak internal details. Auth errors SHALL use generic detail to prevent user enumeration.

| Field | Source |
|-------|--------|
| `extensions.code` | Application `Result.Error.Code` |
| `title` | HTTP status reason phrase |
| `status` | HTTP status code |
| `detail` | Safe user-facing message |

#### Scenario: Auth failure returns ProblemDetails
- GIVEN login with invalid credentials
- WHEN `POST /auth/login`
- THEN 401 with `title: "Unauthorized"`, `status: 401`, generic `detail`, and `extensions.code: "AUTH_INVALID_CREDENTIALS"`

#### Scenario: Unhandled exception returns safe 500
- GIVEN an unhandled exception in any endpoint
- WHEN the exception propagates
- THEN 500 ProblemDetails with generic "Internal Server Error" detail — no stack trace, no internal message

### Requirement: Cancellation Is Not A 500 Error
`OperationCanceledException` and `TaskCanceledException` caused by a cancelled request MUST NOT be emitted as noisy 500 responses or error telemetry. The handler SHOULD treat request-aborted cancellations as expected shutdown paths.

#### Scenario: Client disconnect does not become a 500
- GIVEN a request is aborted by the client
- WHEN the handler observes `OperationCanceledException`
- THEN no 500 ProblemDetails is written for that cancellation path

### Requirement: Result-to-HTTP Mapping
The API layer MUST map Application `Result.Error.Code` to HTTP status codes. The Application layer SHALL NOT reference HTTP concepts. The mapping:

| Error Code Pattern | HTTP Status | Status |
|--------------------|-------------|--------|
| `AUTH_INVALID_CREDENTIALS`, `AUTH_TOKEN_EXPIRED`, `AUTH_TOKEN_REUSE_DETECTED` | 401 | Active |
| `AUTH_REFRESH_TOKEN_MISSING`, `VALIDATION_ERROR` | 400 | Active |
| `NOT_FOUND` | 404 | Active |
| `CONFLICT` | 409 | Active |
| `AUTH_USER_BLOCKED`, `AUTH_TOKEN_REVOKED` | 401 | Reserved/future-proof; not emitted by current endpoints |
| Unknown / unhandled | 500 | Active |

#### Scenario: Result failure produces ProblemDetails
- GIVEN handler returns `Result.Failure("AUTH_TOKEN_EXPIRED", ...)`
- WHEN the API layer maps the result
- THEN 401 ProblemDetails with `extensions.code: "AUTH_TOKEN_EXPIRED"`

### Requirement: Global Exception Handler
The API MUST register an `IExceptionHandler` that catches all unhandled exceptions and returns safe 500 ProblemDetails. Exceptions SHALL be logged server-side via `ILogger`. Internal details MUST NOT reach the response body.

#### Scenario: Exception handler catches and returns safe response
- GIVEN a `NullReferenceException` thrown in a controller action
- WHEN the `IExceptionHandler` processes it
- THEN 500 ProblemDetails with generic detail returned; full exception logged via `ILogger`

### Requirement: Status Code Pages
The API MUST enable `UseStatusCodePages()` so framework-generated status codes (404 from routing, 405 from method mismatch) also produce ProblemDetails responses, not empty or HTML bodies.

#### Scenario: 404 from routing returns ProblemDetails
- GIVEN `GET /nonexistent`
- WHEN the framework returns 404
- THEN response body is a 404 ProblemDetails, not empty
