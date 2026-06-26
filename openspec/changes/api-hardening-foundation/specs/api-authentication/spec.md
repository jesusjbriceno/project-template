# Delta for api-authentication

## ADDED Requirements

### Requirement: Auth Response Metadata
All auth controller actions MUST declare `[ProducesResponseType]` attributes for every possible HTTP status code. Error response types SHALL reference `ProblemDetails`.

| Action | Success | Errors |
|--------|---------|--------|
| `POST /auth/login` | 200 `TokenResponse` | 400, 401 `ProblemDetails` |
| `POST /auth/refresh` | 200 `TokenResponse` | 400, 401 `ProblemDetails` |
| `POST /auth/logout` | 204 | 400 `ProblemDetails` |

#### Scenario: Login action metadata complete
- GIVEN `AuthController.Login` action
- WHEN inspecting attributes
- THEN `[ProducesResponseType(typeof(TokenResponse), 200)]`, `[ProducesResponseType(typeof(ProblemDetails), 400)]`, and `[ProducesResponseType(typeof(ProblemDetails), 401)]` are present

## MODIFIED Requirements

### Requirement: Login Endpoint
`POST /auth/login` MUST accept `{ email, password }`, validate credentials via `LoginCommandHandler`, return access token in body + refresh token in HttpOnly cookie. Error responses SHALL use ProblemDetails with `extensions.code` and generic detail. (Previously: ad-hoc `{ code, message }` anonymous objects)

| Condition | Status | Body | Cookie |
|-----------|--------|------|--------|
| Valid credentials | 200 | `{ accessToken, expiresIn }` | `refreshToken` set |
| Invalid credentials | 401 | ProblemDetails, generic detail, `extensions.code: "AUTH_INVALID_CREDENTIALS"` | None |

#### Scenario: Valid login
- GIVEN a seeded user with correct email and password
- WHEN `POST /auth/login`
- THEN 200 with signed JWT and refresh token cookie

#### Scenario: Generic 401 — no enumeration
- GIVEN nonexistent email, wrong password, or deactivated user
- WHEN `POST /auth/login`
- THEN all return identical 401 ProblemDetails shape and detail message

### Requirement: Refresh Endpoint
`POST /auth/refresh` MUST read cookie, validate token, rotate (revoke old, issue new), and set rotated cookie. Error responses SHALL use ProblemDetails with `extensions.code`. (Previously: ad-hoc `{ code, message }` anonymous objects)

| Condition | Status | Behavior |
|-----------|--------|----------|
| Valid cookie | 200 | New access token + rotated refresh cookie |
| Missing cookie | 400 | ProblemDetails, `extensions.code: "AUTH_REFRESH_TOKEN_MISSING"` |
| Expired token | 401 | ProblemDetails, `extensions.code: "AUTH_TOKEN_EXPIRED"`, clear cookie |
| Revoked token reused | 401 | ProblemDetails, `extensions.code: "AUTH_TOKEN_REUSE_DETECTED"`, revoke family, clear cookie, log security event |

#### Scenario: Successful rotation
- GIVEN valid refresh token cookie
- WHEN `POST /auth/refresh`
- THEN old token revoked, new access + refresh tokens issued

#### Scenario: Reuse revokes entire family
- GIVEN token family F with already-revoked T1 and active T2
- WHEN T1 presented again
- THEN T2 also revoked, all cookies cleared, security log written

### Requirement: Logout Endpoint
`POST /auth/logout` MUST revoke the entire token family and clear cookie via `Set-Cookie: refreshToken=; Max-Age=0`. Return 204. Missing cookie returns 400 ProblemDetails with `extensions.code: "AUTH_REFRESH_TOKEN_MISSING"`. (Previously: ad-hoc `{ code, message }` anonymous object)

#### Scenario: Successful logout
- GIVEN valid refresh token cookie
- WHEN `POST /auth/logout`
- THEN 204, cookie cleared, all family tokens revoked
