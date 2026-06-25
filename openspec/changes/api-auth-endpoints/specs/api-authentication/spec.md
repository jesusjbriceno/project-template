# api-authentication Specification

## Purpose

Backend-only auth endpoints: login, refresh, logout. JWT HS256 access tokens (15-min lifetime) + refresh-token rotation via HttpOnly/Secure/SameSite=Strict cookies.

## Requirements

### Requirement: Login Endpoint

`POST /auth/login` MUST accept `{ email, password }`, validate credentials via `LoginCommandHandler`, return access token in body + refresh token in HttpOnly cookie.

| Condition | Status | Body | Cookie |
|-----------|--------|------|--------|
| Valid credentials | 200 | `{ accessToken, expiresIn }` | `refreshToken` set |
| Invalid credentials (wrong password, no such email, or deactivated user) | 401 | Generic message only | None |

#### Scenario: Valid login
- GIVEN a seeded user with correct email and password
- WHEN `POST /auth/login`
- THEN 200 with signed JWT and refresh token cookie

#### Scenario: Generic 401 — no enumeration
- GIVEN nonexistent email, wrong password, or deactivated user
- WHEN `POST /auth/login`
- THEN all return identical 401 body structure and message

### Requirement: Refresh Endpoint

`POST /auth/refresh` MUST read cookie, validate token, rotate (revoke old, issue new), and set rotated cookie.

| Condition | Status | Behavior |
|-----------|--------|----------|
| Valid cookie | 200 | New access token + rotated refresh cookie |
| Missing cookie | 400 | `AUTH_REFRESH_TOKEN_MISSING` |
| Expired token | 401 | `AUTH_TOKEN_EXPIRED`, clear cookie |
| Revoked token reused | 401 | `AUTH_TOKEN_REUSE_DETECTED`, revoke entire family, clear cookie, log security event |

#### Scenario: Successful rotation
- GIVEN valid refresh token cookie
- WHEN `POST /auth/refresh`
- THEN old token revoked, new access + refresh tokens issued

#### Scenario: Reuse revokes entire family
- GIVEN token family F with already-revoked T1 and active T2
- WHEN T1 presented again
- THEN T2 also revoked, all cookies cleared, security log written

### Requirement: Logout Endpoint

`POST /auth/logout` MUST revoke the entire token family and clear cookie via `Set-Cookie: refreshToken=; Max-Age=0`. Return 204. Missing cookie returns 400 `AUTH_REFRESH_TOKEN_MISSING`.

#### Scenario: Successful logout
- GIVEN valid refresh token cookie
- WHEN `POST /auth/logout`
- THEN 204, cookie cleared, all family tokens revoked

### Requirement: JWT Access Token Contract

Access tokens SHALL use HS256, 15-min lifetime. Claims: `sub`, `email`, `roles[]`, `jti`, `iss`, `aud`, `exp`, `iat`. `Jwt__Secret` env var (≥32 bytes) MUST serve as signing key. Invalid/expired tokens SHALL return 401.

#### Scenario: Token claims extraction
- GIVEN validly-signed access token
- WHEN JWT middleware validates
- THEN `sub`, `email`, `roles` populate `UserSession`
- AND expired or malformed tokens rejected with 401

### Requirement: Refresh Token Cookie Policy

Cookie SHALL be set with: HttpOnly, Secure, SameSite=Strict, Path=/auth. Max-Age SHALL be configurable via `Jwt__RefreshTokenDays` (default 604800s / 7 days).

#### Scenario: Cookie security properties
- GIVEN any auth response setting the cookie
- THEN JavaScript-inaccessible, HTTPS-only, CSRF-protected, path-scoped to `/auth`

### Requirement: Password Policy

Passwords SHALL be validated against configurable policy: minimum length 12, must include uppercase, lowercase, digit, and special character. Policy failure returns the same generic 401 as invalid credentials — reason never disclosed to caller.

#### Scenario: Short password
- GIVEN password under configured minimum length
- WHEN login attempted
- THEN 401 generic — policy failure reason not disclosed

### Requirement: Startup Configuration Validation

`Jwt__Secret` (≥32 bytes), `Jwt__Issuer`, and `Jwt__Audience` MUST be validated at startup. Missing or invalid configuration SHALL block startup with a descriptive error message.

#### Scenario: Missing secret
- GIVEN `Jwt__Secret` empty or under 32 bytes
- WHEN application starts
- THEN startup fails with configuration error
