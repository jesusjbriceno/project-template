# Tasks: Infrastructure EF Core

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1600 (7 slices × ~230 avg) |
| 400-line budget risk | Medium |
| Chained PRs recommended | Yes |
| Suggested split | PR 1a → PR 1b → PR 1c → PR 2 → PR 3 → PR 4 → PR 5 |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Base Branch |
|------|------|-----------|-------------|
| 1a | Typed ID converters/comparers only + layer-isolation + ID tests | PR 1a | feature/infrastructure-ef-core (tracker) |
| 1b | Value-object converters (Email, PermissionKey, DeletionPolicy) + Domain EF materialization prep (User, RefreshToken) + VO tests | PR 1b | PR 1a branch |
| 1c | ApplicationDbContext, audit interceptor, DI, NullUserSession, PostgreSQL fixture/DbContext tests | PR 1c | PR 1b branch |
| 2 | Foundation entity configs: User/Role/Permission + round-trip tests | PR 2 | PR 1c branch |
| 3 | Relation entity configs: UserRole/RolePermission/MenuItem/RefreshToken + tests | PR 3 | PR 2 branch |
| 4 | Base + User/Role repositories + CRUD/paged/includeDeleted tests | PR 4 | PR 3 branch |
| 5 | Permission/RefreshToken/MenuItem repositories + tests | PR 5 | PR 4 branch |

## Phase 1a: Typed ID Converters & Comparers (PR 1a) — ~384 lines

- [x] 1a.1 **RED**: Write `LayerIsolationTests` — compile check that Domain/Application have zero EF Core/Npgsql refs
- [x] 1a.2 **RED**: Write converter/comparer tests — 7 ID round-trips + 2 comparer edge cases (9 tests total)
- [x] 1a.3 **GREEN**: Add `Microsoft.EntityFrameworkCore.Design` to `Project.Infrastructure.csproj`
- [x] 1a.4 **GREEN**: Create 7 ID converter + 7 ID comparer classes (14 files)
- [x] 1a.5 **VERIFY**: Run `dotnet test` — all 11 PR1a tests pass (no database required)

## Phase 1b: Value-Object Converters + Domain EF Prep (PR 1b) — ~118 lines

_Requires PR 1a files in working tree. PR 1b files are currently staged in `_pr1b_deferred/`._

- [x] 1b.1 **RESTORE**: Move `_pr1b_deferred/` files back to working tree
- [x] 1b.2 **RED**: Write `ValueObjectConverterTests` — Email normalization, PermissionKey/PermissionKey round-trip, DeletionPolicy 4-bool round-trip
- [x] 1b.3 **GREEN**: Create `EmailConverter` — Email ↔ string via Email.Create normalization
- [x] 1b.4 **GREEN**: Create `PermissionKeyConverter` — PermissionKey ↔ string
- [x] 1b.5 **GREEN**: Create `DeletionPolicyConverter` — DeletionPolicy ↔ string (JSONB via System.Text.Json)
- [x] 1b.6 **GREEN**: Add private parameterless constructor to `User` for EF Core materialization
- [x] 1b.7 **GREEN**: Add private parameterless constructor + private setters to `RefreshToken` for EF Core materialization
- [x] 1b.8 **VERIFY**: Run `dotnet test` — all PR 1a + PR 1b tests pass (no database required)

## Phase 1c: DbContext Core Infrastructure (PR 1c) — ~327 lines

_Requires PR 1a + PR 1b files in working tree. PR 1c files are currently staged in `_pr1c_deferred/`._

- [x] 1c.1 **RESTORE**: Move `_pr1c_deferred/` files back to working tree
- [x] 1c.2 **RED**: Write `PostgresFixture` + `ApplicationDbContextTests` — DI resolution and `CanCreateDatabase` against Testcontainers
- [x] 1c.3 **GREEN**: Add `Testcontainers.PostgreSql` to `Project.IntegrationTests.csproj`
- [x] 1c.4 **GREEN**: Create `AuditTimestampInterceptor` — resolves IClock, sets CreatedAt/UpdatedAt on Add/Modify
- [x] 1c.5 **GREEN**: Create `NullUserSession` — IUserSession returning null (scaffold until security slice)
- [x] 1c.6 **GREEN**: Create `ApplicationDbContext` — NoTracking, SplitQuery, NpgsqlRetryingExecutionStrategy, audit interceptor, all 7 DbSets
- [x] 1c.7 **GREEN**: Create `DependencyInjection.cs` — `AddInfrastructure` extension binding IClock→SystemClock, DbContext, interceptors, NullUserSession
- [x] 1c.8 **VERIFY**: Run `dotnet test` — all PR 1a + PR 1b + PR 1c tests pass (Docker required for Postgres container)
- [ ] 1c.9 **DEFERRED**: Run `dotnet ef migrations add InitialSchema` — generate full schema migration (deferred: requires entity configs from Phase 2-3 for correct schema; see apply-progress remediation)

## Phase 2: Foundation Entity Configurations (PR 2)

- [x] 2.1 **RED**: Write User/Role/Permission round-trip integration tests — CRUD, Email normalization, Name unique, Key unique
- [x] 2.2 **GREEN**: Create `UserConfiguration` — `users` table, PK UserId, unique Email index, soft-delete `HasQueryFilter(e => !e.IsDeleted)`
- [x] 2.3 **GREEN**: Create `RoleConfiguration` — `roles` table, PK RoleId, unique Name index, soft-delete filter
- [x] 2.4 **GREEN**: Create `PermissionConfiguration` — `permissions` table, PK PermissionId, unique Key index (no soft-delete)
- [ ] 2.5 **GREEN**: Run `dotnet ef migrations add AddFoundationConfigs`

## Phase 3: Relation Entity Configurations (PR 3)

- [x] 3.1 **RED**: Write composite-key uniqueness, self-ref FK, TokenHash index integration tests
- [x] 3.2 **GREEN**: Create `UserRoleConfiguration` — `user_roles`, composite PK {UserId, RoleId}, FK→User, FK→Role
- [x] 3.3 **GREEN**: Create `RolePermissionConfiguration` — `role_permissions`, composite PK {RoleId, PermissionId}, FK→Role, FK→Permission
- [x] 3.4 **GREEN**: Create `MenuItemConfiguration` — `menu_items`, PK MenuItemId, self-ref FK on ParentId, soft-delete filter
- [x] 3.5 **GREEN**: Create `RefreshTokenConfiguration` — `refresh_tokens`, PK RefreshTokenId, unique TokenHash index, FamilyId index, ExpiresAt index
- [ ] 3.6 **GREEN**: Run `dotnet ef migrations add AddRelationConfigs`

## Phase 4: Repository Core (PR 4)

- [x] 4.1 **RED**: Write `BaseRepository` tests — GetByIdAsync (includeDeleted: true/false), AddAsync, Update, Delete, GetPagedAsync
- [x] 4.2 **RED**: Write `UserRepository` tests — GetByEmailAsync, ExistsAsync, GetActiveSuperadminsAsync
- [x] 4.3 **GREEN**: Create `BaseRepository<TEntity, TId>` — IgnoreQueryFilters when includeDeleted=true, PagedResult via Skip/Take
- [x] 4.4 **GREEN**: Create `UserRepository` — GetByEmailAsync, ExistsAsync, GetActiveSuperadminsAsync
- [x] 4.5 **GREEN**: Create `RoleRepository` — role-specific queries

## Phase 5: Remaining Repositories (PR 5)

- [x] 5.1 **RED**: Write PermissionRepository, RefreshTokenRepository, MenuItemRepository integration tests
- [x] 5.2 **GREEN**: Create `PermissionRepository` — GetByKeyAsync
- [x] 5.3 **GREEN**: Create `RefreshTokenRepository` — GetByTokenHashAsync, GetByFamilyIdAsync, RevokeFamilyAsync (ExecuteUpdate)
- [x] 5.4 **GREEN**: Create `MenuItemRepository` — GetChildrenAsync, GetRootItemsAsync
