# application-layer Specification

## Purpose

Application-layer Clean Architecture boundary contracts: Result-based outcomes, CQRS contracts, repository interfaces for Domain aggregates, FluentValidation direction, auth/session lifecycle, refresh-token family revocation, and controlled Superadmin enforcement. Zero EF Core, ASP.NET, or Infrastructure references. This foundation enables every subsequent use-case slice.

## Requirements

### Requirement: Result Pattern

All operations MUST return `Result` or `Result<T>`. Exceptions SHALL NOT be used for control flow. Results MUST carry error codes (e.g., `"NOT_FOUND"`, `"UNAUTHORIZED"`) and human-readable messages. `Result<T>` SHALL expose `IsSuccess`, `IsFailure`, `Value`, and `Error`. Implicit conversions from `T` and error tuples are RECOMMENDED.

#### Scenario: Success and failure

- GIVEN valid operation → `Result<T>.Success(value)` → IsSuccess=true, Value present
- GIVEN operation fails → `Result.Failure("NOT_FOUND","msg")` → IsFailure=true, Error.Code set

#### Scenario: No control-flow exceptions

- GIVEN any failed operation → Result returned, never thrown → caller inspects IsSuccess

### Requirement: CQRS Contracts

`ICommand` SHALL represent writes returning `Result`. `IQuery<TResponse>` SHALL represent reads returning `Result<TResponse>`. `ICommandHandler<TCommand>` and `IQueryHandler<TQuery,TResponse>` define handler signatures with `CancellationToken` support. No external mediator dependency required.

#### Scenario: Command and query signatures

- GIVEN `CreateUserCommand : ICommand` → `ICommandHandler<CreateUser>.Handle(command, ct)` returns `Task<Result>`
- GIVEN `GetUserQuery : IQuery<UserDto>` → `IQueryHandler<GetUserQuery,UserDto>.Handle(query, ct)` returns `Task<Result<UserDto>>`

### Requirement: Repository Interfaces

A shared `IBaseRepository<TEntity, TId>` SHALL expose common CRUD primitives (`GetByIdAsync`, `AddAsync`, `Update`, `Delete`) and paginated search (`GetPagedAsync`). Per-aggregate interfaces (`IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IMenuItemRepository`) SHALL inherit from the base contract and add only aggregate-specific queries. All repository interfaces MUST use Domain entity types and strongly-typed IDs. MUST NOT reference EF Core, IQueryable, DbContext, or Infrastructure types.

#### Scenario: Base repository contract

- GIVEN `IBaseRepository<TEntity, TId>` → exposes `GetByIdAsync(TId, bool includeDeleted = false, ct): TEntity?`, `AddAsync(TEntity, ct)`, `Update(TEntity)`, `Delete(TEntity)`, `GetPagedAsync(PageRequest, bool includeDeleted = false, ct): PagedResult<TEntity>`
- GIVEN `IUserRepository : IBaseRepository<User, UserId>` → inherits CRUD + paged search; adds `GetByEmailAsync`, `ExistsAsync`, `GetActiveSuperadminsAsync`

#### Scenario: Paginated search without infrastructure leakage

- GIVEN `PageRequest` with Page / PageSize → validated at construction (Page >= 1, 1 <= PageSize <= 100)
- GIVEN `PagedResult<T>` with Items / TotalCount / TotalPages / HasNextPage / HasPreviousPage → immutable, no IQueryable or Expression
- GIVEN handler calls `GetPagedAsync(request, ct)` → returns `PagedResult<T>` without leaking EF Core Skip/Take or SQL dialect

#### Scenario: CRUD and invariant pre-loading

- GIVEN `IUserRepository` → inherits `GetByIdAsync(UserId, ct): User?`, `AddAsync(User, ct)` from base
- Update/Delete follow Domain lifecycle (deactivate, not physical delete unless allowed)
- Exposes `GetActiveSuperadminsAsync(ct): IReadOnlyCollection<User>` for enforcement context

#### Scenario: Include deleted records

- GIVEN a soft-deleted User exists in the database
- WHEN `GetByIdAsync(userId, includeDeleted: true)` is called
- THEN the soft-deleted User SHALL be returned
- AND calling `GetByIdAsync(userId)` (default) SHALL NOT return the soft-deleted User

### Requirement: Validation Direction

Commands and queries SHOULD declare FluentValidation validators. A marker interface (`IValidated`) MAY signal pipeline validation. Concrete validators for specific commands belong in use-case slices, not in the foundation layer.

#### Scenario: Validated command marker

- GIVEN `command : IValidated` → pipeline MAY validate via `AbstractValidator<T>` before handler execution

### Requirement: Auth and Session Boundaries

`IUserSession` SHALL expose current `UserId`, `Email`, and actor roles. `ITokenService` SHALL declare a family-revocation contract: upon `RefreshTokenReuseSignalException`, Application MUST invoke `RevokeFamily(familyId)`. `IJwtTokenService` SHALL expose `GenerateAccessToken(User, IReadOnlyCollection<Role>)`, `GenerateRefreshToken()`, and `ValidateAccessToken(string)`. Application MUST use these abstractions for auth flows — no raw token handling.

#### Scenario: Audit identity and family revocation

- GIVEN handler needs `CreatedBy` → obtains identity from `IUserSession`, never HTTP context
- `ITokenService` SHALL expose `RevokeFamilyAsync(Guid, CancellationToken)` for family revocation
- `IJwtTokenService` SHALL expose issuance and validation without leaking JWT key material or crypto details to Application

#### Scenario: Token issuance abstraction

- GIVEN an auth handler needing to issue tokens
- WHEN it calls `IJwtTokenService.GenerateAccessToken(user, roles)`
- THEN it receives a signed JWT string via the contract, never touching key material

### Requirement: Superadmin Enforcement

`ISuperadminEnforcementContext` SHALL pre-load `activeSuperadmins` and `actorRoles`. Use cases MUST use this context before Domain methods that require these collections. Generic `IUserSession` MUST NOT substitute for Superadmin enforcement.

#### Scenario: Last-superadmin enforcement contract

- `ISuperadminEnforcementContext` SHALL pre-load `activeSuperadmins` and `actorRoles` — the contract exists now.
  Runtime handler flow (loading context, invoking Domain invariants, mapping to `Result.Failure` on last-Superadmin violation) is deferred to future use-case slices.

### Requirement: Security Boundaries

Application MUST NOT bypass Domain invariants. Auth/session contracts MUST NOT leak Infrastructure/API concerns (HTTP context, JWT claims, raw tokens). Application SHALL NOT handle or persist raw tokens.

#### Scenario: Controlled enforcement only

- GIVEN Superadmin gate → Application MUST use `ISuperadminEnforcementContext` (specific, controlled), never generic `IUserSession`

### Requirement: Out-of-Scope Boundaries

Foundation MUST NOT include: concrete handlers beyond contract proof, EF Core or repository implementations, JWT issuance/validation logic, login/logout flows, API endpoints, or the MediatR NuGet package. Last-superadmin handler orchestration remains deferred to future use-case slices.

#### Scenario: Clean compile boundary

- GIVEN Application project → `dotnet build` succeeds with zero EF Core, ASP.NET Core, or Infrastructure assembly references

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
