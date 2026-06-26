# Delta for application-layer

## MODIFIED Requirements

### Requirement: Result Pattern
All operations MUST return `Result` or `Result<T>`. Exceptions SHALL NOT be used for control flow. Results MUST carry error codes (e.g., `"AUTH_INVALID_CREDENTIALS"`, `"NOT_FOUND"`) and human-readable messages. `Result<T>` SHALL expose `IsSuccess`, `IsFailure`, `Value`, and `Error`. Implicit conversions from `T` and error tuples are RECOMMENDED. The Application layer SHALL remain HTTP-agnostic: Result error codes MUST NOT encode HTTP semantics. HTTP status code mapping SHALL be owned exclusively by the API layer. (Previously: HTTP-agnostic boundary not explicitly stated)

#### Scenario: Success and failure
- GIVEN valid operation → `Result<T>.Success(value)` → IsSuccess=true, Value present
- GIVEN operation fails → `Result.Failure("AUTH_TOKEN_EXPIRED","msg")` → IsFailure=true, Error.Code set

#### Scenario: No control-flow exceptions
- GIVEN any failed operation → Result returned, never thrown → caller inspects IsSuccess

#### Scenario: HTTP-agnostic error codes
- GIVEN `Result.Failure("AUTH_TOKEN_EXPIRED", ...)`
- WHEN inspected in the Application layer
- THEN the error code carries no HTTP status reference — the API layer owns status code mapping
