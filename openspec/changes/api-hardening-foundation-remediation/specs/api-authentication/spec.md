# Delta for api-authentication

## Purpose

Cover the two auth scenarios that lacked runtime evidence in the prior verification pass:
the deactivated-user branch of the generic-401 enumeration guard, and the `ProducesResponseType`
metadata contract for the `AuthController` actions.

## MODIFIED Requirements

### Requirement: Login Endpoint

`POST /auth/login` MUST accept `{ email, password }`, validate credentials via
`LoginCommandHandler`, return access token in body + refresh token in HttpOnly cookie.
Error responses SHALL use ProblemDetails with `extensions.code` and generic detail.
The "Generic 401 — no enumeration" scenario MUST include an isolated runtime test case for
the deactivated-user branch; that test MUST exercise the same shape, status, and `extensions.code`
assertions as the wrong-password and nonexistent-user cases.
(Previously: Scenario listed deactivated user in prose but no integration test covered the
deactivated-user branch.)

#### Scenario: Valid login
- GIVEN a seeded user with correct email and password
- WHEN `POST /auth/login`
- THEN 200 with signed JWT and refresh token cookie

#### Scenario: Generic 401 — no enumeration
- GIVEN nonexistent email, wrong password, or deactivated user
- WHEN `POST /auth/login`
- THEN all return identical 401 ProblemDetails shape and detail message
- AND the same `extensions.code` (`AUTH_INVALID_CREDENTIALS`) is returned for every branch
- AND the deactivated-user branch is asserted by its own dedicated integration test

## ADDED Requirements

### Requirement: Auth Response Metadata Runtime Evidence

A runtime test SHALL reflect on the `AuthController` actions and assert each action declares
the `[ProducesResponseType]` attributes required by the table below. The test SHALL also parse
`/openapi/v1.json` (Development) and confirm the operation advertises the same success and
error response types.

| Action | Success | Errors |
|--------|---------|--------|
| `POST /auth/login` | 200 `TokenResponse` | 400, 401 `ProblemDetails` |
| `POST /auth/refresh` | 200 `TokenResponse` | 400, 401 `ProblemDetails` |
| `POST /auth/logout` | 204 | 400 `ProblemDetails` |

#### Scenario: Login action metadata complete
- GIVEN `AuthController.Login` action
- WHEN inspecting attributes
- THEN `[ProducesResponseType(typeof(TokenResponse), 200)]`,
  `[ProducesResponseType(typeof(ProblemDetails), 400)]`, and
  `[ProducesResponseType(typeof(ProblemDetails), 401)]` are present

#### Scenario: Refresh and logout metadata complete
- GIVEN `AuthController.Refresh` and `AuthController.Logout` actions
- WHEN inspecting attributes
- THEN `Refresh` declares 200 `TokenResponse` plus 400 and 401 `ProblemDetails`
- AND `Logout` declares 204 plus 400 `ProblemDetails`

#### Scenario: OpenAPI document advertises login response types
- GIVEN the OpenAPI document is fetched from `/openapi/v1.json` in Development
- WHEN the test reads the `POST /auth/login` operation
- THEN the operation declares a 200 response of `TokenResponse`
- AND the operation declares 400 and 401 responses of `ProblemDetails`
