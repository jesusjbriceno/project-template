# Delta for application-layer

## Purpose

Provide runtime evidence for the two Result-pattern scenarios that lacked covering tests
in the prior verification pass: a failed caller path that proves no exception escapes, and
an architectural-boundary check that proves the Application layer is HTTP-agnostic.

## MODIFIED Requirements

### Requirement: Result Pattern

All operations MUST return `Result` or `Result<T>`. Exceptions SHALL NOT be used for control
flow. Results MUST carry error codes (e.g., `"AUTH_INVALID_CREDENTIALS"`, `"NOT_FOUND"`) and
human-readable messages. `Result<T>` SHALL expose `IsSuccess`, `IsFailure`, `Value`, and
`Error`. Implicit conversions from `T` and error tuples are RECOMMENDED. The Application
layer SHALL remain HTTP-agnostic: Result error codes MUST NOT encode HTTP semantics. HTTP
status code mapping SHALL be owned exclusively by the API layer. The "No control-flow
exceptions" and "HTTP-agnostic error codes" scenarios MUST be backed by runtime tests — one
that drives a handler through a failed path and asserts no exception is thrown, and one that
inspects the Application layer to assert it has no `Microsoft.AspNetCore.*` reference.
(Previously: Scenarios defined in prose but no runtime test exercised a failed caller path
or asserted the HTTP-agnostic compile boundary.)

#### Scenario: Success and failure
- GIVEN valid operation → `Result<T>.Success(value)` → IsSuccess=true, Value present
- GIVEN operation fails → `Result.Failure("AUTH_TOKEN_EXPIRED","msg")` → IsFailure=true, Error.Code set

#### Scenario: No control-flow exceptions
- GIVEN any failed operation → Result returned, never thrown → caller inspects IsSuccess
- AND a runtime test that drives an auth handler with bad credentials executes without
  throwing
- AND that test asserts the returned `Result` is a failure with the expected
  `extensions.code` (via the API-layer mapper, never inside Application)

#### Scenario: HTTP-agnostic error codes
- GIVEN `Result.Failure("AUTH_TOKEN_EXPIRED", ...)`
- WHEN inspected in the Application layer
- THEN the error code carries no HTTP status reference — the API layer owns status code mapping
- AND a runtime test asserts the Application project compiles without `Microsoft.AspNetCore.*`
  references
- AND the same test (or a sibling) asserts the `ErrorCodes` constants hold no numeric HTTP
  status suffix
