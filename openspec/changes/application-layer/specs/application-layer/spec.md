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

Per-aggregate interfaces SHALL exist: `IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IMenuItemRepository`. MUST use Domain entity types and strongly-typed IDs. MUST expose async CRUD and invariant pre-loading methods. MUST NOT reference EF Core types or return DTOs.

#### Scenario: CRUD and invariant pre-loading

- GIVEN `IUserRepository` → exposes `GetByIdAsync(UserId, ct): User?`, `AddAsync(User, ct): void`
- Update/Delete follow Domain lifecycle (deactivate, not physical delete unless allowed)
- Exposes `GetActiveSuperadminsAsync(ct): IReadOnlyCollection<User>` for enforcement context

### Requirement: Validation Direction

Commands and queries SHOULD declare FluentValidation validators. A marker interface (`IValidated`) MAY signal pipeline validation. Concrete validators for specific commands belong in use-case slices, not in the foundation layer.

#### Scenario: Validated command marker

- GIVEN `command : IValidated` → pipeline MAY validate via `AbstractValidator<T>` before handler execution

### Requirement: Auth and Session Boundaries

`IUserSession` SHALL expose current `UserId`, `Email`, and actor roles. `ITokenService` SHALL declare a family-revocation contract: upon `RefreshTokenReuseSignalException`, Application MUST invoke `RevokeFamily(familyId)`. No raw token handling — abstract contracts only.

#### Scenario: Audit identity and family revocation

- GIVEN handler needs `CreatedBy` → obtains identity from `IUserSession`, never HTTP context
- GIVEN `Rotate()` throws `RefreshTokenReuseSignalException` → Application calls `ITokenService.RevokeFamily(familyId)`

### Requirement: Superadmin Enforcement

`ISuperadminEnforcementContext` SHALL pre-load `activeSuperadmins` and `actorRoles`. Use cases MUST use this context before Domain methods that require these collections. Generic `IUserSession` MUST NOT substitute for Superadmin enforcement.

#### Scenario: Last-superadmin guard

- GIVEN deactivation targeting the last active Superadmin → handler loads context → Domain signals invariant violation → handler returns `Result.Failure`

### Requirement: Security Boundaries

Application MUST NOT bypass Domain invariants. Auth/session contracts MUST NOT leak Infrastructure/API concerns (HTTP context, JWT claims, raw tokens). Application SHALL NOT handle or persist raw tokens.

#### Scenario: Controlled enforcement only

- GIVEN Superadmin gate → Application MUST use `ISuperadminEnforcementContext` (specific, controlled), never generic `IUserSession`

### Requirement: Out-of-Scope Boundaries

Foundation MUST NOT include: concrete handlers beyond contract proof, EF Core DbContext or repository implementations, JWT issuance/validation logic, login/logout flows, API endpoints, or the MediatR NuGet package.

#### Scenario: Clean compile boundary

- GIVEN Application project → `dotnet build` succeeds with zero EF Core, ASP.NET Core, or Infrastructure assembly references
