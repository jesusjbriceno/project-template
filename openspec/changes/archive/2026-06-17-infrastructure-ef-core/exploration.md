# Exploration: Infrastructure EF Core

## Current State

The Clean Architecture monorepo has three of four backend layers built:

| Layer | Status | Files | Tests |
|-------|--------|-------|-------|
| **Domain** | ✅ Complete & Archived | 27 `.cs` files — 7 entities (User, Role, Permission, RefreshToken, MenuItem, 2 junctions), 2 value objects (Email, PermissionKey), 7 strongly-typed IDs wrapping `Guid`, audit base, clock, deletion policy, 8 domain exceptions | 241 green |
| **Application** | ✅ Foundation Complete & Archived | 20 `.cs` files — `Result`/`Result<T>`/`Error`, CQRS contracts (`ICommand`, `IQuery<T>`, handler interfaces), 5 per-aggregate repository interfaces inheriting from `IBaseRepository<TEntity,TId>`, pagination types, security boundaries (`IUserSession`, `ISuperadminEnforcementContext`, `ITokenService`), `IValidated` marker | 67 green |
| **Infrastructure** | ❌ **Empty shell** | Zero `.cs` source files. Only `Project.Infrastructure.csproj` with EF Core 10.0.9 + Npgsql 10.0.2 references, and build artifacts in `obj/`/`bin/` | 0 |
| **API Controllers** | ⚠️ Scaffold only | `Program.cs` with `/health` endpoint + placeholder DI comments | 2 integration tests |
| **MigrationService** | ⚠️ Scaffold only | `Program.cs` exit-immediately placeholder | 0 |

**The gap**: The Infrastructure project has NuGet packages installed but zero implementation. It references `Project.Application` (transitively Domain via that dependency). All Application-layer contracts wait for concrete Infrastructure implementations: 5 repository interfaces, `ITokenService`, `ISuperadminEnforcementContext`, `IUserSession`. EF Core DbContext, entity configurations, migrations, and seeding do not exist.

**Dependency graph** (relevant slice):
```
Project.Domain (BCL only, zero deps)
    ↑
Project.Application (+ FluentValidation)
    ↑
Project.Infrastructure (+ EF Core 10.0.9, Npgsql 10.0.2)  ← THIS CHANGE
    ↑
Project.Api.Controllers (Web SDK, references Infrastructure + Application)
Project.MigrationService (Worker SDK, references Infrastructure)
```

Docker Compose spins PostgreSQL 17-alpine with a named volume (`pgdata`), a migration service (scaffold), and the API (health endpoint only). The migration service depends on `db: service_healthy`; the API depends on `migration: service_completed_successfully`.

## Affected Areas

- **`apps/api/src/Project.Infrastructure/`** — **primary target**, currently empty. Will receive:
  - `Data/ApplicationDbContext.cs` — DbContext with `DbSet<>` for all 7 entities, global `NoTracking`/`SplitQuery`/`ExecutionStrategy` config, soft-delete query filter
  - `Data/Configurations/` — 7 `IEntityTypeConfiguration<T>` classes (one per entity) for Fluent API mappings: table names, PK conversions, value object conversions, navigation relationships, indexes
  - `Data/Converters/` — value converters and comparers for strongly-typed IDs (7 classes wrapping `Guid`), `Email`, `PermissionKey`, `DeletionPolicy` (JSON column)
  - `Data/Repositories/` — 6 concrete classes: `BaseRepository<TEntity,TId>` + 5 per-aggregate repos (`UserRepository`, `RoleRepository`, `PermissionRepository`, `RefreshTokenRepository`, `MenuItemRepository`)
  - `Security/` — `TokenService`, `SuperadminEnforcementContext`, `UserSession`
  - `Data/DependencyInjection.cs` — extension method for Infrastructure DI registration
  - `GlobalUsings.cs` — shared usings

- **`apps/api/src/Project.MigrationService/Program.cs`** — rewrite scaffold: wire `AddDbContext<ApplicationDbContext>`, connection string from config, `MigrationWorker` hosted service (apply migrations on start, seed system roles + permissions + superadmin, then exit)

- **`apps/api/src/Project.Api.Controllers/Program.cs`** — uncomment DI line for `AddDbContext<ApplicationDbContext>(...)` and repository registrations; no endpoint or middleware changes in this change

- **`apps/api/tests/Project.IntegrationTests/`** — will receive:
  - PostgreSQL Testcontainers setup
  - Integration tests for DbContext resolution, entity mapping (CRUD round-trips), repository implementations, soft-delete filter behavior, migration application
  - Infrastructure-specific unit tests for value converters

- **`apps/api/tests/Project.UnitTests/`** / **`Project.ApplicationTests/`** — no changes needed; Domain and Application contracts are already fully tested and verified. Infrastructure tests are integration-level (real DB required for EF Core behavior).

- **`openspec/specs/`** — will receive a new `infrastructure-ef-core` delta spec in the subsequent `sdd-spec` phase

- **`docker-compose.yml`** / **`infra/docker/migration.Dockerfile`** — migration Dockerfile already copies Infrastructure project; compose already depends on migration before API. May need minor env var adjustments for connection string format.

## Approach: Needs Chained Slicing

### The 400-Line Budget Reality

A monolithic Infrastructure implementation would be **~2000–2500 lines** of source code + tests — **5×–6× the review budget**. This MUST be sliced.

Each slice must have: clear start state, clear finish state, autonomous scope, verification (tests pass with real PostgreSQL), and reasonable rollback. The chain uses feature-branch-chain strategy: `Slice 1` targets `develop` (or a feature tracker), `Slice 2` targets `Slice 1`'s branch, etc.

---

### Approach A: Bottom-Up — Configuration First, Queries Last

Build Infrastructure in dependency order: value converters → entity configurations → DbContext → repos → migration service → security.

**Slice 1 — DbContext Core + Value Converters** (~350 lines)
- `ApplicationDbContext` with `DbSet<>` properties, `OnModelCreating` stub, global `NoTracking`, `SplitQuery`, `ExecutionStrategy`
- 7 strongly-typed ID value converters (`UserIdConverter`, `RoleIdConverter`, etc.) + comparers
- `EmailConverter` (string ↔ `Email`), `PermissionKeyConverter` (string ↔ `PermissionKey`)
- `DeletionPolicy` JSON converter for PostgreSQL `jsonb`
- `GlobalUsings.cs`, `DependencyInjection.cs` (AddInfrastructure extension)
- Tests: value converter round-trip unit tests + integration test proving DI registers context against Testcontainers PostgreSQL

**Slice 2 — Entity Configurations (Foundations)** (~350 lines)
- `UserConfiguration` — table `users`, PK `Id` (HasConversion), `Email` owned/converted, `PasswordHash`, `SecurityStamp`, `IsActive`, `LastLoginAt`, audit fields, soft-delete filter, `UserRoles` collection nav
- `RoleConfiguration` — table `roles`, PK, `Name` (unique index), `IsSystem`, audit fields, soft-delete filter with system-role guard, `RolePermissions` nav
- `PermissionConfiguration` — table `permissions`, PK, `Key` conversion + unique index, `Description`, `Category`, audit fields
- Tests: integration tests for CRUD round-trips on User, Role, Permission (create → read → update → soft-delete)

**Slice 3 — Entity Configurations (Junctions + Hierarchy + Token)** (~300 lines)
- `UserRoleConfiguration` — composite key `{UserId, RoleId}`, FK `User → UserRoles`, FK `Role → UserRoles`, index on `UserId`
- `RolePermissionConfiguration` — composite key `{RoleId, PermissionId}`, FK `Role → RolePermissions`, FK `Permission → RolePermissions`
- `MenuItemConfiguration` — table `menu_items`, PK, `ParentId` self-referencing FK, `SortOrder`, visibility fields, `RequiredPermissionKey`/`RequiredRoleId` conversions
- `RefreshTokenConfiguration` — table `refresh_tokens`, PK, `TokenHash` (64-char index), `FamilyId` (index), `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenHash`
- Tests: integration tests for junction insert/query, hierarchy insert/walk, token CRUD + family queries

**Slice 4a — Repository Implementations: Base + User/Role** (~350 lines)
- `BaseRepository<TEntity, TId>` — `GetByIdAsync`, `AddAsync`, `Update`, `Delete`, `GetPagedAsync` (Skip/Take via EF Core, total count)
- `UserRepository : BaseRepository<User, UserId>` — `GetByEmailAsync` (Email conversion query), `GetActiveSuperadminsAsync` (join UserRoles → Roles where IsSystem+superadmin+IsActive), `ExistsAsync`
- `RoleRepository : BaseRepository<Role, RoleId>` — `GetSystemRolesAsync`, `ExistsAsync`
- Tests: integration tests for base CRUD + paged search + aggregate-specific queries against real PostgreSQL

**Slice 4b — Repository Implementations: Permission + Token + MenuItem** (~250 lines)
- `PermissionRepository` — `GetByKeyAsync` (PermissionKey conversion), `ExistsByKeyAsync`
- `RefreshTokenRepository` — `GetByTokenHashAsync` (hash lookup), `RevokeFamilyAsync` (ExecuteUpdate batch), `GetActiveByFamilyIdAsync`
- `MenuItemRepository` — `GetAllAsync` (full hierarchy), `GetChildrenAsync` (by parent)
- Tests: integration tests for token hash lookup, family revocation, menu hierarchy loading

**Slice 5 — MigrationService Implementation** (~300 lines)
- `MigrationWorker.cs` — `BackgroundService`: pending-migration check, `MigrateAsync()`, execution strategy retry, logs, then `StopApplication()`
- `SeedData.cs` — static seeder: system roles (Superadmin, etc.), permission catalog (~30-40 permissions: users.create/read/update/delete, roles.*, menu.*, etc.), initial superadmin user with bcrypt-hashed password from env var
- `Program.cs` rewrite — wire AddDbContext, connection string `"DefaultConnection"`, MigrationWorker hosted service
- Tests: integration test proving migration applies and seed runs (check role/permission/user counts)

**Slice 6 — Security Implementations** (~250 lines)
- `TokenService` — implements `ITokenService.RevokeFamilyAsync` via `RefreshTokenRepository` + batch ExecuteUpdate
- `SuperadminEnforcementContext` — implements `ISuperadminEnforcementContext`, delegates to `IUserRepository` and `IRoleRepository`
- `UserSession` — implements `IUserSession`, wraps `IHttpContextAccessor` (extracts UserId from claims, resolves Roles; null-safe for unauthenticated)
- Tests: integration tests for token family revocation, superadmin pre-loading

| Pros | Cons |
|------|------|
| Each slice is independently reviewable (250–350 lines) | 6 PRs to land sequentially — higher orchestration overhead |
| Dependency order prevents circular build breaks | Slices 4a/4b depend on slices 1-3; Slice 5 depends on 1-4; Slice 6 depends on repos |
| Aligns with force-chained delivery strategy | Integration test overlap between slices may need careful test isolation |
| Design can be course-corrected after early slices | |

---

### Approach B: Vertical Slice Per Aggregate

Build one aggregate end-to-end per slice: User (DbContext config + User repo + UserRole junction + tests), then Role, then Permission, etc.

| Pros | Cons |
|------|------|
| Each slice is fully self-contained for one aggregate | Duplicate DbContext scaffolding in every PR |
| Easier to see "does User persistence work end-to-end?" | `BaseRepository<T>` appears in Slice 1 but evolves across slices — refactoring tax |
| No blocking dependency between slices | Value converters duplicated or extracted late |
| | Mismatch with Clean Architecture: Infrastructure is a horizontal layer, not vertical |

---

### Approach C: Single PR with Size Exception

Accept the 2000+ line PR and record a `size:exception` in the delivery strategy.

| Pros | Cons |
|------|------|
| No orchestration overhead — one merge | **Review quality collapses** at 2000+ lines — reviewer fatigue is inevitable |
| All Infrastructure lands atomically | Violates force-chained delivery strategy |
| | Architectural mistakes caught late (after merge, not during review) |
| | Reviewer cannot give meaningful line-level feedback on entity configs + repos + migrations in one pass |

---

## Recommendation

**Approach A — Bottom-Up, 6 chained PR slices.**

This is the only approach that aligns with the force-chained delivery strategy and the 400-line review budget. Infrastructure is a horizontal layer with intrinsic dependency ordering (value converters → entity configs → repos → migration service → security). Each slice delivers a coherent, testable, and reviewable unit.

### Detailed Slicing Plan

```
develop (or feature/infrastructure-ef-core tracker)
  ├── PR #1: ef-core-dbcontext    ← DbContext core, value converters, DI (~350 lines)
  │     └── PR #2: ef-core-configs-foundations  ← User/Role/Permission configs (~350 lines)
  │           └── PR #3: ef-core-configs-relations  ← Junctions, MenuItem, RefreshToken (~300 lines)
  │                 └── PR #4a: ef-core-repos-core  ← BaseRepo + UserRepo + RoleRepo (~350 lines)
  │                       └── PR #4b: ef-core-repos-remaining  ← Permission/Token/MenuItem repos (~250 lines)
  │                             └── PR #5: ef-core-migrations  ← MigrationWorker + SeedData (~300 lines)
  │                                   └── PR #6: ef-core-security  ← TokenService + EnforcementContext + UserSession (~250 lines)
```

### What Stays OUT of Scope

- **Concrete CQRS handlers** (use cases: create user, login, token rotation, role management) — future Application-layer slices
- **JWT issuance/validation** — deferred to auth infrastructure slice (needs `Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Password hashing (bcrypt)** — deferred to auth use-case slice; Infrastructure may need `IPasswordHasher` abstraction
- **API endpoints/controllers** — future API-layer slices
- **ProblemDetails/Result HTTP mapping** — API-layer concern
- **MediatR integration** — spec defers; custom CQRS dispatcher works without it
- **Frontend** — Phase 9

## Key Technical Decisions to Make

### 1. Strongly-Typed ID Mapping Strategy

All 7 ID types (`UserId`, `RoleId`, `PermissionId`, `RefreshTokenId`, `MenuItemId`, `UserRoleId`, `RolePermissionId`) wrap `Guid` with `implicit operator Guid`. Two options:

| Option | How | Tradeoff |
|--------|-----|----------|
| **A) HasConversion per property** | `HasConversion(id => id.Value, g => XxxId.From(g))` + `ValueComparer` per ID type | Explicit per-property; 7 converter classes + 7 comparers. Boilerplate but clear. |
| **B) Bulk configure via pre-convention model** | `ConfigureConventions` in `OnModelCreating` registers 7 converters globally | Less boilerplate. Risk: any `Guid` column that shouldn't be an ID gets converted. |

**Recommendation**: Option A per property. Explicit over implicit per project convention. Each entity configuration declares its own PK/FK conversion — no ambiguity.

### 2. Value Object Storage

| Value Object | Storage Strategy | Rationale |
|-------------|------------------|-----------|
| **Email** | Owned type or column conversion `string ↔ Email` | `Email.Value` is a normalized lowercase string. Owned type keeps `Email` as a DB-column group (just one column). Conversion is simpler and sufficient. |
| **PermissionKey** | Column conversion `string ↔ PermissionKey` | `PermissionKey.Value` is a `[a-z][a-z0-9]*\.[a-z][a-z0-9]*` string. Single-column conversion. |
| **DeletionPolicy** | JSON column (`jsonb`) with `System.Text.Json` converter | 4 boolean dimensions — storing as JSON preserves the record shape without separate columns. |

### 3. Junction Entity Key Strategy

`UserRole` and `RolePermission` have composite IDs (`UserRoleId(UserId, RoleId)`, `RolePermissionId(RoleId, PermissionId)`). EF Core can't use these classes directly as keys. Two options:

| Option | How | Tradeoff |
|--------|-----|----------|
| **A) Composite PK via shadow properties** | `HasKey(ur => new { ur.UserId, ur.RoleId })` with HasConversion on each FK | Natural composite key. No synthetic surrogate. Clean. EF Core handles this well. |
| **B) Synthetic Guid PK** | Add a hidden `Id` Guid property, composite unique index on FKs | Simpler EF Core config. But adds an unused surrogate column the Domain doesn't know about. |

**Recommendation**: Option A — composite PK via `HasKey`. The Domain already expresses the composite identity; Infrastructure should honor it.

### 4. Soft-Delete Query Filter

Entities with `DeletionPolicy.SoftDeleteEnabled = true` (User, Role, MenuItem) need a global query filter: `e => e.IsDeleted == false`. This must NOT apply to RefreshToken (lifecycle via revocation/expiration) or Permission (hard-delete only).

The filter is applied in each entity configuration's `Configure` method, not globally — because not all entities support soft-delete.

### 5. RefreshToken Family Revocation

`RevokeFamilyAsync(Guid familyId)` must revoke ALL active tokens in the family. Two implementation options:

| Option | How |
|--------|-----|
| **A) Load-then-update** | Load all active tokens for family → set `RevokedAt` → `SaveChangesAsync` |
| **B) ExecuteUpdate bulk** | `_db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null).ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow))` |

**Recommendation**: Option B — single SQL statement, no materialization, consistent with EF Core 7+ patterns and the NoTracking default.

### 6. Paginated Search Implementation

`IBaseRepository<TEntity,TId>.GetPagedAsync` receives a `PageRequest` (Page, PageSize) and returns `PagedResult<T>`. Infrastructure implements:

```csharp
var query = _dbSet.AsQueryable(); // soft-delete filter already applied via global query filter
var totalCount = await query.CountAsync(ct);
var items = await query.Skip((request.Page - 1) * request.PageSize)
                       .Take(request.PageSize)
                       .ToListAsync(ct);
return new PagedResult<TEntity>(items, totalCount, request.Page, request.PageSize);
```

`IQueryable<T>` is used internally in Infrastructure (never leaks to Application). This is the correct Clean Architecture boundary.

## Testing Strategy

### Integration Tests (Primary)

Infrastructure behavior is inherently database-coupled. Unit-testing EF Core configurations against an in-memory provider produces false confidence (different SQL dialect, no real constraint enforcement). The integration test strategy:

- **PostgreSQL Testcontainers** — spin up `postgres:17-alpine` per test class/collection
- **Apply migrations** on container start — prove migrations work
- **Test real EF Core behavior**: CRUD round-trips, soft-delete filter, composite keys, unique constraints, cascade behavior, ExecuteUpdate/ExecuteDelete, paginated queries
- **Test repository implementations**: verify they return Domain entities correctly hydrated, navigation properties loaded when included, aggregate-specific queries return correct results
- **Test seeding**: verify initial migration + seed produces expected system roles, permissions, superadmin

Test project: `Project.IntegrationTests` (already exists with `WebApplicationFactory` + xUnit; needs `Testcontainers.PostgreSql` NuGet package).

### Unit Tests (Secondary — value converters only)

Value converters are pure functions — they deserve unit tests (no DB needed):
- `UserIdConverter`: round-trip `UserId → Guid → UserId`, rejects `Guid.Empty`
- `EmailConverter`: round-trip `Email → string → Email`, normalization
- `PermissionKeyConverter`: round-trip validation
- `DeletionPolicy` JSON converter: serialize/deserialize

### TDD Discipline

Per `openspec/config.yaml` `strict_tdd: true`, every behavior must have a failing test before code. For Infrastructure:

1. **Write integration test** that proves the behavior is missing (e.g., "DbContext throws when not configured" → configure it → test passes)
2. For value converters: write unit test → implement converter → green
3. For entity configs: write integration test that queries for entity after insert → implement config → green
4. For repos: write integration test calling repo method → implement repo → green

## Risks

- **EF Core 10.0 preview behavior**: .NET 10 and EF Core 10.0.9 are pre-release. API surface may shift. Mitigation: pin exact versions; verify against `dotnet ef` CLI compatibility.
- **Npgsql type mapping surprises**: PostgreSQL has native types (UUID, JSONB, timestamptz) that map to .NET types. `DateTimeOffset` → `timestamptz` should work; `Guid` → `uuid` is standard. Verify with Testcontainers, not assumptions.
- **Composite key complexity**: `UserRole` and `RolePermission` composite keys require careful FK configuration and navigation setup. Getting this wrong produces silent data corruption (wrong associations). Mitigation: integration tests with explicit FK assertions.
- **Soft-delete filter leakage**: If the global query filter is misconfigured, queries may return soft-deleted records (security: deleted users appear in listings; deleted roles re-assignable). Mitigation: integration test that inserts soft-deleted record and asserts it's excluded from queries.
- **RefreshToken hash collision surface**: `GetByTokenHashAsync` uses an exact-match index on a 64-char hex string. SHA-256 collisions are astronomically unlikely, but the system must not fail silently if they occur. Mitigation: unique index on `TokenHash`.
- **Migration ordering and Docker timing**: Migration service must complete before API starts. If the migration container fails silently, the API starts against an empty database. Mitigation: migration service exits non-zero on failure; Docker `depends_on: service_completed_successfully` enforces this.
- **Seeding idempotency**: `SeedData` must be safe to run multiple times (container restarts). Mitigation: check existence before insert; use `INSERT ... ON CONFLICT DO NOTHING` or EF Core `AnyAsync` guard.
- **Reviewer fatigue across 6 chained PRs**: Even with 400-line slices, 6 sequential reviews is a lot. Mitigation: each PR is truly autonomous — reviewer can evaluate one slice without holding the next in mind. Clear PR descriptions referencing the chain.

## Ready for Proposal

**Yes.** The exploration has identified:
- Complete inventory of what Infrastructure must implement (DbContext, 7 entity configs, 7 value converters, 5 repos, 3 security implementations, MigrationWorker, SeedData)
- Precise dependency graph and layer constraints
- 6-slice chained PR plan fitting the 400-line review budget
- Testing strategy (integration-first with PostgreSQL Testcontainers, unit tests for converters)
- Key architectural decisions (strongly-typed ID mapping, composite keys, soft-delete filter, ExecuteUpdate for family revocation)
- Explicit out-of-scope boundaries (no use-case handlers, no JWT issuance, no API endpoints)

The orchestrator should launch `sdd-propose` next to create a formal change proposal with Scope, Approach, Rollback Plan, and the chained slicing strategy.
