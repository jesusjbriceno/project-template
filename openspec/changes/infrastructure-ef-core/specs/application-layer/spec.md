# Delta for application-layer

## MODIFIED Requirements

### Requirement: Repository Interfaces

A shared `IBaseRepository<TEntity, TId>` SHALL expose common CRUD primitives (`GetByIdAsync`, `AddAsync`, `Update`, `Delete`) and paginated search (`GetPagedAsync`). `GetByIdAsync` and `GetPagedAsync` SHALL accept an optional `includeDeleted` parameter (default `false`). When `includeDeleted: true`, the query SHALL include soft-deleted records; default behavior hides them. Per-aggregate interfaces (`IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IMenuItemRepository`) SHALL inherit from the base contract and add only aggregate-specific queries. All repository interfaces MUST use Domain entity types and strongly-typed IDs. MUST NOT reference EF Core, IQueryable, DbContext, or Infrastructure types.

(Previously: `GetByIdAsync` and `GetPagedAsync` had no `includeDeleted` parameter — soft-deleted records were implicitly excluded with no opt-in.)

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
