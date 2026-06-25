# Delta for application-layer

## MODIFIED Requirements

### Requirement: Auth and Session Boundaries

`IUserSession` SHALL expose current `UserId`, `Email`, and actor roles. `ITokenService` SHALL declare a family-revocation contract: upon `RefreshTokenReuseSignalException`, Application MUST invoke `RevokeFamily(familyId)`. `IJwtTokenService` SHALL expose `GenerateAccessToken(User, IReadOnlyCollection<Role>)`, `GenerateRefreshToken()`, and `ValidateAccessToken(string)`. Application MUST use these abstractions for auth flows — no raw token handling.

(Previously: only `ITokenService` existed with family revocation. `IJwtTokenService` did not exist. Handler-level reuse signal handling was deferred.)

#### Scenario: Audit identity and family revocation

- GIVEN handler needs `CreatedBy` → obtains identity from `IUserSession`, never HTTP context
- `ITokenService` SHALL expose `RevokeFamilyAsync(Guid, CancellationToken)` for family revocation
- `IJwtTokenService` SHALL expose issuance and validation without leaking JWT key material or crypto details to Application

#### Scenario: Token issuance abstraction

- GIVEN an auth handler needing to issue tokens
- WHEN it calls `IJwtTokenService.GenerateAccessToken(user, roles)`
- THEN it receives a signed JWT string via the contract, never touching key material

### Requirement: Out-of-Scope Boundaries

Foundation MUST NOT include: concrete handlers beyond contract proof, EF Core or repository implementations, JWT issuance/validation logic, login/logout flows, API endpoints, or the MediatR NuGet package. Last-superadmin handler orchestration remains deferred to future use-case slices.

(Previously: both handler-level refresh-token reuse exception handling and last-superadmin handler orchestration were deferred. Refresh-token handling is now fulfilled by auth use-case handlers.)

#### Scenario: Clean compile boundary

- GIVEN Application project → `dotnet build` succeeds with zero EF Core, ASP.NET Core, or Infrastructure assembly references

## ADDED Requirements

### Requirement: Auth Use-Case Contracts

`LoginCommand`, `RefreshTokenCommand`, `LogoutCommand` SHALL implement `ICommand`. Handlers returning `Result<T>` SHALL implement `ICommandHandler<TCommand, TResult>`. Handlers returning `Result` (no value) SHALL implement `ICommandHandler<TCommand>`. Handlers MUST catch `RefreshTokenReuseSignalException` and call `ITokenService.RevokeFamilyAsync(familyId)`. `TokenPairDto` SHALL carry `AccessToken` (string), `ExpiresInSeconds` (int), and `RefreshToken` (string raw value) for the API layer to set the HttpOnly cookie.

#### Scenario: Login handler signature

- GIVEN `LoginCommand { Email, Password } : ICommand`
- WHEN `LoginCommandHandler.Handle(command, ct)` invoked
- THEN returns `Result<TokenPairDto>` with access token on success

#### Scenario: Refresh handler reuse detection

- GIVEN a refresh handler processing a token
- WHEN `RefreshToken.Rotate()` throws `RefreshTokenReuseSignalException`
- THEN handler catches it, calls `RevokeFamilyAsync(familyId)`, returns failure with `AUTH_TOKEN_REUSE_DETECTED`

#### Scenario: Logout handler signature

- GIVEN `LogoutCommand { RefreshTokenRaw } : ICommand`
- WHEN `LogoutCommandHandler.Handle(command, ct)` invoked
- THEN returns `Result` (void), token family revoked

### Requirement: Auth Validation

Auth commands SHALL declare FluentValidation validators implementing `AbstractValidator<T>`. `LoginCommandValidator` SHALL enforce email format and non-empty password. `RefreshTokenCommandValidator` and `LogoutCommandValidator` SHALL validate that the refresh token value is present and non-whitespace.

#### Scenario: Login validation

- GIVEN `LoginCommand` with empty email or whitespace-only password
- WHEN validated
- THEN fails with descriptive error

### Requirement: Auth Error Codes

`ErrorCodes.Auth` SHALL expose: `InvalidCredentials`, `UserBlocked`, `TokenExpired`, `TokenRevoked`, `TokenReuseDetected`, `RefreshTokenMissing`. All SHALL be prefixed `AUTH_`. Used by auth handlers in `Result.Failure`, never by Domain.

#### Scenario: Error code isolation

- GIVEN a login failure
- WHEN handler creates `Result.Failure`
- THEN error code is `ErrorCodes.Auth.InvalidCredentials` — no Domain-layer constants used
