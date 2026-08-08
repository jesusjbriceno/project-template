# Delta for api-error-contract

## Purpose

Remediate the framework-generated 404/405 status-code-pages path so it honors the public
`extensions.code` contract, leaks no internal details, and never double-writes a response body
that downstream middleware already produced.

## MODIFIED Requirements

### Requirement: Status Code Pages

The API MUST enable a status-code-pages handler so framework-generated status codes
(404 from routing, 405 from method mismatch) also produce RFC 7807 ProblemDetails responses,
not empty or HTML bodies. The handler MUST inject the stable public codes
`ROUTING_NOT_FOUND` for 404 and `METHOD_NOT_ALLOWED` for 405 into `extensions.code`, MUST use
generic `detail` text (no route, no stack trace, no internal exception type), and MUST NOT
write a second response body when downstream middleware already produced a ProblemDetails.
(Previously: Bare `UseStatusCodePages()` did not assign `extensions.code`, did not guarantee
generic detail, and did not assert no double write.)

#### Scenario: 404 from routing returns ProblemDetails
- GIVEN `GET /nonexistent`
- WHEN the framework returns 404
- THEN response body is a 404 ProblemDetails, not empty

#### Scenario: 404 from routing exposes ROUTING_NOT_FOUND
- GIVEN `GET /nonexistent`
- WHEN the framework returns 404
- THEN the ProblemDetails `extensions.code` equals `ROUTING_NOT_FOUND`
- AND the ProblemDetails `detail` is generic (no route, no stack, no internal exception)

#### Scenario: 405 from method mismatch returns ProblemDetails
- GIVEN an existing route only declares `GET`
- WHEN `POST /that/route`
- THEN response body is a 405 ProblemDetails, not empty
- AND the `Allow` response header lists the supported method

#### Scenario: 405 from method mismatch exposes METHOD_NOT_ALLOWED
- GIVEN an existing route only declares `GET`
- WHEN `POST /that/route`
- THEN the ProblemDetails `extensions.code` equals `METHOD_NOT_ALLOWED`
- AND the ProblemDetails `detail` is generic

#### Scenario: Status handler does not double-write
- GIVEN a downstream middleware already produced a ProblemDetails body
- WHEN the status-code handler executes
- THEN the response body remains the original ProblemDetails unchanged
- AND `extensions.code` is preserved (not overwritten with a default)

#### Scenario: Status handler does not leak internal details
- GIVEN the framework produces a 404 or 405 with internal exception context
- WHEN the status-code handler emits the ProblemDetails
- THEN the response body MUST NOT contain the route, file path, line number, exception type,
  or stack frame
- AND only the stable public `code` plus a generic `detail` are exposed
