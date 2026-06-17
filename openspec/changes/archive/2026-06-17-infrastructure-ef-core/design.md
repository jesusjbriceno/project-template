# Design: Infrastructure EF Core

## Technical Approach

Bottom-up layering inside `Project.Infrastructure` keeps EF Core sealed behind the layer boundary. Domain + Application stay BCL/FluentValidation-only; outward artifacts are the five concrete repositories and `AddInfrastructure(IServiceCollection)`. `ApplicationDbContext` configures `NoTracking`/`SplitQuery`/`NpgsqlRetryingExecutionStrategy` globally, applies `IEntityTypeConfiguration<T>`, and stamps audit timestamps via a `SaveChangesInterceptor` that resolves `IClock` from a created scope. PostgreSQL types are native (`uuid`, `text`, `jsonb`, `timestamptz`). Migrations live in Infrastructure; `MigrationService` scaffold stays untouched. Soft-delete is a per-entity query filter; the application-layer delta's `IncludeDeleted` opt-in uses `IgnoreQueryFilters()`.

## Architecture Decisions

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 1 | Strongly-typed ID persistence | `HasConversion` + `ValueComparer<XxxId>` per ID | Comparer needed for value-typed wrappers. |
| 2 | Value object storage | `Email`/`PermissionKey` → `text`; `DeletionPolicy` → `jsonb` | One column for Email; 4-bool record stays JSON. |
| 3 | Soft-delete filter scope | Per-entity `HasQueryFilter(!IsDeleted)` on User/Role/MenuItem only | Matches per-entity `DeletionPolicy.SoftDeleteEnabled`. |
| 4 | `IncludeDeleted` opt-in | `bool includeDeleted = false`; `true` chains `.IgnoreQueryFilters()` | Honors application-layer delta. |
| 5 | Audit timestamp writer | `SaveChangesInterceptor` resolves `IClock`; sets audit fields on Add/Modify | Only Infrastructure-aware entry point. |
| 6 | `DateTimeOffset` ↔ PostgreSQL | Npgsql native `timestamptz`; no custom converter | Npgsql 10 maps `DateTimeOffset` natively. |
| 7 | Composite junction keys | `HasKey(ur => new { UserId, RoleId })` / `HasKey(rp => new { RoleId, PermissionId })`; no surrogate | Domain expresses composite identity. |
| 8 | Migration ownership + `IClock` | Migrations in Infrastructure; `MigrationService` stays scaffold; `AddInfrastructure` binds `IClock → SystemClock` | Spec excludes seed/bootstrap; Domain owns clock abstraction. |
| 9 | Layer-isolation test | xUnit builds Domain/Application independently, fails on `EntityFrameworkCore*`/`Npgsql*` refs | CI gate for spec's first scenario. |

## Data Flow

    IUserRepository ──► UserRepo ──► ApplicationDbContext (scoped)
    ITokenService   ──► (future)    │  DbSet<User/Role/MenuItem>  ──HasQueryFilter(!IsDeleted)
                                    │  DbSet<Permission/Refresh>  (no filter)
                                    │  DbSet<UserRole/RolePerm>   ──HasKey(...)
                                    │       │
                                    │       ▼
                                    │  SaveChangesInterceptor → IClock → audit fields
                                    ▼
                                  PostgreSQL 17

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `apps/api/src/Project.Infrastructure/Project.Infrastructure.csproj` | Modify | Add `Microsoft.EntityFrameworkCore.Design`. |
| `apps/api/src/Project.Infrastructure/{GlobalUsings,DependencyInjection}.cs` | Create | EF/Npgsql aliases; `AddInfrastructure` extension. |
| `apps/api/src/Project.Infrastructure/Data/ApplicationDbContext.cs` | Create | DbContext, `DbSet`s, `NoTracking`, interceptor registration. |
| `apps/api/src/Project.Infrastructure/Data/Interceptors/AuditTimestampInterceptor.cs` | Create | `SaveChangesInterceptor` for audit fields. |
| `apps/api/src/Project.Infrastructure/Data/Converters/IdConverter.cs` (x7) + `Comparers/IdComparer.cs` (x7) | Create | Converter + comparer per ID type. |
| `apps/api/src/Project.Infrastructure/Data/Converters/{Email,PermissionKey,DeletionPolicy}Converter.cs` | Create | VO conversions; `DeletionPolicy` → `jsonb`. |
| `apps/api/src/Project.Infrastructure/Data/Configurations/{User,Role,Permission,RefreshToken,MenuItem,UserRole,RolePermission}Configuration.cs` (7 files) | Create | Fluent API mappings per spec table. |
| `apps/api/src/Project.Infrastructure/Data/Repositories/BaseRepository.cs` | Create | Generic base; `includeDeleted` on read paths. |
| `apps/api/src/Project.Infrastructure/Data/Repositories/{User,Role,Permission,RefreshToken,MenuItem}Repository.cs` (5 files) | Create | Per-aggregate repos. |
| `apps/api/src/Project.Infrastructure/Security/NullUserSession.cs` | Create | Scaffold `IUserSession` returning `null`. |
| `apps/api/tests/Project.IntegrationTests/Project.IntegrationTests.csproj` | Modify | Add `Testcontainers.PostgreSql`. |
| `apps/api/tests/Project.IntegrationTests/Infrastructure/{LayerIsolationTests,PostgresFixture,ApplicationDbContextTests}.cs` (3 files) | Create | Layer isolation; fixture; integration tests. |
| `apps/api/src/Project.Infrastructure/Data/Migrations/*` (InitialSchema) | Generated | `dotnet ef migrations add InitialSchema`; not auto-applied. |

## Interfaces / Contracts

```csharp
public virtual async Task<TEntity?> GetByIdAsync(
    TId id, bool includeDeleted = false, CancellationToken ct = default)
{
    var query = Set.AsQueryable();
    if (includeDeleted) query = query.IgnoreQueryFilters();
    return await query.FirstOrDefaultAsync(BuildIdPredicate(id), ct);
}
```

Only `IgnoreQueryFilters` call site; `IQueryable` never escapes. `NullUserSession` keeps the interceptor compiling until the security slice.

## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | ID/VO converter + comparer; audit interceptor with fake `IClock` | Pure functions. |
| Integration | Layer isolation + `ApplicationDbContextTests` against `postgres:17-alpine` Testcontainer: CRUD round-trip, VO/ID round-trip, audit, composite-key uniqueness, soft-delete hidden by default, `IgnoreQueryFilters` opt-in | One container per `[Collection("Postgres")]`, migrations once; `TRUNCATE ... CASCADE` per test class. |

## Migration / Rollout

Initial migration generated by `dotnet ef migrations add InitialSchema --project Project.Infrastructure --startup-project Project.Api.Controllers`. Not auto-applied — `MigrationService` stays a scaffold. Rollback = `git revert` of the chained PR slice (or `dotnet ef database update 0` locally).

## Force-Chained PR Slicing (≤ 400 changed lines each)

| Slice | Scope | LoC | Verification |
|-------|-------|-----|--------------|
| **#1a** Typed ID converters | 7 ID converters, 7 ID comparers, layer-isolation + ID tests, csproj | ~384 | Build green; layer-isolation green; ID converter tests green. |
| **#1b** VO converters + EF materialization prep | `Email`, `PermissionKey`, `DeletionPolicy` converters; private EF constructors/setters for materialization | ~118 | VO converter tests green; Domain/Application boundary still clean. |
| **#1c** DbContext core | `ApplicationDbContext`, interceptor, DI, `NullUserSession`, PostgreSQL fixture + DbContext tests | ~327 | DI resolves context; `EnsureCreated` works against Testcontainers. |
| **#2** Foundations configs | `User/Role/PermissionConfiguration` + tests | ~330 | User/Role/Permission round-trip. |
| **#3** Relations configs | `UserRole/RolePermission/MenuItem/RefreshTokenConfiguration` + tests | ~300 | Composite-key uniqueness, self-ref FK, token hash index. |
| **#4** Base + User/Role repos | `BaseRepository`, `UserRepository`, `RoleRepository` + tests | ~330 | CRUD + paged + superadmin pre-load + email lookup. |
| **#5** Remaining repos | Three repos + `RevokeFamilyAsync` (ExecuteUpdate) + tests | ~290 | `GetByKey`, `GetByTokenHash`, menu hierarchy. |

Feature-branch-chain: each slice targets the previous slice's branch; `Slice 1a` targets the tracker branch. Tests travel with each slice. No `size:exception`.

## Open Questions

None blocking. Deferred: `MigrationWorker`/seed/superadmin (`ef-core-migrations`); `TokenService`/`SuperadminEnforcementContext`/real `IUserSession` (`ef-core-security`).
