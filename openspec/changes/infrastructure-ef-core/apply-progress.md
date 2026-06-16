# Apply Progress: Infrastructure EF Core — 3-Way Split

**Change**: infrastructure-ef-core
**Date**: 2026-06-15 (original), split 2-way 2026-06-15, split 3-way 2026-06-15
**Phase**: 1b — Value-Object Converters + Domain EF Materialization Prep (PR 1b)
**Status**: READY FOR REVIEW (PR 1b)

## Split Rationale

Original PR 1 was ~1013 changed lines after remediation, exceeding the 400-line review budget.
First split produced PR 1a (~506 lines, still over budget) and PR 1b (~327 lines).
Second split further divides PR 1a into two slices because the 2-way split left PR 1a above budget:

| Slice | Scope | Lines | Base Branch |
|-------|-------|-------|-------------|
| **PR 1a** | Typed ID converters/comparers + layer isolation + ID tests | ~384 | feature/infrastructure-ef-core (tracker) |
| **PR 1b** | Value-object converters (Email, PermissionKey, DeletionPolicy) + Domain EF prep (User, RefreshToken) + VO tests | ~118 | PR 1a branch |
| **PR 1c** | ApplicationDbContext, interceptor, DI, NullUserSession, PostgresFixture/DbContext tests | ~327 | PR 1b branch |

PR 1b files staged in `_pr1b_deferred/`. PR 1c files staged in `_pr1c_deferred/`.

## Completed Tasks (PR 1a)

| Task | Description | Status |
|------|-------------|--------|
| 1a.1 | LayerIsolationTests — Domain/Application zero EF Core refs | ✅ |
| 1a.2 | ID converter/comparer tests — 7 ID round-trips + 2 comparer edges (9 tests) | ✅ |
| 1a.3 | EF Core Design package | ✅ |
| 1a.4 | 7 ID converters + 7 ID comparers (14 files) | ✅ |
| 1a.5 | dotnet test verification — 11/11 pass | ✅ |

## TDD Cycle Evidence (PR 1a)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1a.1 | `LayerIsolationTests.cs` | Integration | ✅ 308/308 | ✅ Guard test | ✅ Passed | ➖ Single | ✅ Clean |
| 1a.2 | `ConverterTests.cs` | Unit (in IntegrationTests) | N/A (new) | ✅ Compile fail | ✅ All 9 pass | ✅ 2 comparer edges | ✅ Clean |
| 1a.3 | N/A (package) | — | N/A | N/A | ✅ Added | — | — |
| 1a.4 | N/A (converters) | — | N/A | N/A | ✅ All 14 files | — | ✅ Expression tree fixes |

### Test Summary (PR 1a)
- **Total new tests**: 11 (2 layer isolation + 9 converter/comparer)
- **Tests passing**: 11/11 via `dotnet test` (no database required)
- **Layers used**: Integration (11)

## Files Changed (PR 1a)

### Created (Infrastructure — 14 files)

**7 ID Converters:**
| File | Lines | Description |
|------|-------|-------------|
| `Data/Converters/UserIdConverter.cs` | 12 | UserId ↔ Guid ValueConverter |
| `Data/Converters/RoleIdConverter.cs` | 12 | RoleId ↔ Guid ValueConverter |
| `Data/Converters/PermissionIdConverter.cs` | 12 | PermissionId ↔ Guid ValueConverter |
| `Data/Converters/RefreshTokenIdConverter.cs` | 12 | RefreshTokenId ↔ Guid ValueConverter |
| `Data/Converters/MenuItemIdConverter.cs` | 12 | MenuItemId ↔ Guid ValueConverter |
| `Data/Converters/UserRoleIdConverter.cs` | 19 | UserRoleId ↔ string (composite scaffold) |
| `Data/Converters/RolePermissionIdConverter.cs` | 19 | RolePermissionId ↔ string (composite scaffold) |

**7 ID Comparers:**
| File | Lines | Description |
|------|-------|-------------|
| `Data/Converters/UserIdComparer.cs` | 15 | UserId ValueComparer |
| `Data/Converters/RoleIdComparer.cs` | 15 | RoleId ValueComparer |
| `Data/Converters/PermissionIdComparer.cs` | 15 | PermissionId ValueComparer |
| `Data/Converters/RefreshTokenIdComparer.cs` | 15 | RefreshTokenId ValueComparer |
| `Data/Converters/MenuItemIdComparer.cs` | 15 | MenuItemId ValueComparer |
| `Data/Converters/UserRoleIdComparer.cs` | 17 | UserRoleId ValueComparer |
| `Data/Converters/RolePermissionIdComparer.cs` | 18 | RolePermissionId ValueComparer |

### Created (Tests — 2 files)
| File | Lines | Description |
|------|-------|-------------|
| `Infrastructure/LayerIsolationTests.cs` | 55 | Clean Architecture boundary verification |
| `Infrastructure/Data/Converters/ConverterTests.cs` | 96 | ID round-trip + comparer unit tests (9 test methods) |

### Modified (1 file)
| File | Change |
|------|--------|
| `Project.Infrastructure.csproj` | Added `Microsoft.EntityFrameworkCore.Design 10.0.9` (+1 line) |

## PR 1b Files (Deferred — staged in `_pr1b_deferred/`)

### Infrastructure Converters (3 files, ~55 lines)
| File | Lines | Description |
|------|-------|-------------|
| `Data/Converters/EmailConverter.cs` | 16 | Email ↔ string via Email.Create normalization |
| `Data/Converters/PermissionKeyConverter.cs` | 15 | PermissionKey ↔ string |
| `Data/Converters/DeletionPolicyConverter.cs` | 24 | DeletionPolicy ↔ JSONB string |

### Domain Entity EF Prep (2 files, ~24 lines)
| File | Lines | Description |
|------|-------|-------------|
| `Domain/Entities/User.cs` | 8 | Private parameterless constructor for EF Core materialization |
| `Domain/Entities/RefreshToken.cs` | 16 | Private parameterless constructor + private setters |

### VO Tests (1 file, ~50 lines)
| File | Lines | Description |
|------|-------|-------------|
| `Infrastructure/Data/Converters/ValueObjectConverterTests.cs` | 46 | Email normalization, PermissionKey/DeletionPolicy round-trip (3 test methods) |

## PR 1c Files (Deferred — staged in `_pr1c_deferred/`)

### Infrastructure (4 files, ~202 lines)
| File | Lines | Description |
|------|-------|-------------|
| `Data/ApplicationDbContext.cs` | 61 | DbContext with NoTracking, SplitQuery, 7 DbSets |
| `Data/Interceptors/AuditTimestampInterceptor.cs` | 64 | SaveChangesInterceptor for audit timestamps |
| `DependencyInjection.cs` | 59 | AddInfrastructure extension method |
| `Security/NullUserSession.cs` | 18 | Scaffold IUserSession |

### Tests (3 files, ~124 lines)
| File | Lines | Description |
|------|-------|-------------|
| `Infrastructure/PostgresFixture.cs` | 60 | PostgreSQL 17 Testcontainers fixture |
| `Infrastructure/PostgresCollection.cs` | 10 | xUnit collection definition |
| `Infrastructure/ApplicationDbContextTests.cs` | 54 | DI resolution + EnsureCreated + NoTracking tests |

### Modified (1 file)
| File | Change |
|------|--------|
| `Project.IntegrationTests.csproj` | Add `Testcontainers.PostgreSql 4.6.0` (reverted in PR 1a/1b) |

## Deviations from Design

1. **Composite ID converters retained**: UserRoleId/RolePermissionId converters store as strings (e.g., `"guid1_guid2"`). These are scaffolding until Phase 2-3 shadow composite key configs replace the surrogate approach.
2. **Domain modifications deferred to PR 1b**: Private parameterless constructors for User and RefreshToken were moved from PR 1a to PR 1b to keep PR 1a focused on Infrastructure converters only.
3. **Migration deferred (1c.9)**: Cannot generate correct schema without entity configurations. Will be generated in Phase 2-3 after entity configs are verified via Testcontainers.

## Issues Found

1. **CS8122 (expression tree 'is')**: ValueComparer constructors — fixed by simplifying null checks.
2. **CS0834 (statement lambda)**: Composite ID converters — fixed by using expression-bodied lambdas.
3. **PR 1a at 384 lines**: Under the 400-line budget. The 3-way split solved the overage from the 2-way split (was ~506 lines).

## Remaining Tasks

- [x] PR 1c: Restore `_pr1c_deferred/` files + implement DbContext core ✅
- [x] Phase 2: Foundation Entity Configurations (User/Role/Permission) ✅
- [x] Phase 3: Relation Entity Configurations (UserRole/RolePermission/MenuItem/RefreshToken) ✅
- [x] Phase 4: Repository Core (Base + User/Role repos) ✅
- [ ] Phase 5: Remaining Repositories (Permission/RefreshToken/MenuItem + tests + DI)

## PR 1a Verification

```
$ dotnet test apps/api/tests/Project.IntegrationTests --filter "LayerIsolationTests|IdConverterTests"
Total tests: 11 — Passed: 11, Failed: 0, Skipped: 0
```

---

## Completed Tasks (PR 1b) — 2026-06-15

| Task | Description | Status |
|------|-------------|--------|
| 1b.1 | RESTORE: Move `_pr1b_deferred/` files back to working tree | ✅ |
| 1b.2 | RED: Write ValueObjectConverterTests (6 test methods) | ✅ |
| 1b.3 | GREEN: Create EmailConverter | ✅ |
| 1b.4 | GREEN: Create PermissionKeyConverter | ✅ |
| 1b.5 | GREEN: Create DeletionPolicyConverter | ✅ |
| 1b.6 | GREEN: Add private parameterless constructor to User | ✅ |
| 1b.7 | GREEN: Add private parameterless constructor + private setters to RefreshToken | ✅ |
| 1b.8 | VERIFY: Run `dotnet test` — all PR 1a + PR 1b tests pass | ✅ |

## TDD Cycle Evidence (PR 1b)

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1b.2 | `ValueObjectConverterTests.cs` | Unit (in IntegrationTests) | ✅ 319/319 | ✅ Compile failed (CS0246 ×3) | ✅ 3/3 pass | ✅ 6/6 pass (+3 edge cases) | ✅ Clean |
| 1b.3 | `EmailConverter.cs` | — | N/A (new) | N/A | ✅ All tests pass | See 1b.2 | ✅ Clean |
| 1b.4 | `PermissionKeyConverter.cs` | — | N/A (new) | N/A | ✅ All tests pass | See 1b.2 | ✅ Clean |
| 1b.5 | `DeletionPolicyConverter.cs` | — | N/A (new) | N/A | ✅ All tests pass | See 1b.2 | ✅ Clean |
| 1b.6 | `User.cs` (add private ctor) | — | ✅ 239/239 UnitTests | N/A (structural) | ✅ Build green, all tests green | ➖ Single | ✅ CS8618 pragma |
| 1b.7 | `RefreshToken.cs` (add private ctor + setters) | — | ✅ 239/239 UnitTests | N/A (structural) | ✅ Build green, all tests green | ➖ Single | ✅ CS8618 pragma, private set |

### Test Summary (PR 1b)
- **New tests written**: 6 (3 happy path + 3 triangulation/edge cases)
- **Tests passing**: 325/325 (67 Application + 239 Unit + 19 Integration) via `dotnet test apps/api`
- **Layers used**: Unit (6, via IntegrationTests project — no database required)
- **Approval tests**: None — no refactoring tasks
- **Pure functions created**: 0 (converters are adapter classes, not pure functions)

### Triangulation Details

| Original Test (Happy Path) | Triangulation (Edge/Alt Case) |
|----------------------------|-------------------------------|
| `Email_RoundTrip_NormalizesAndPreservesValue` ("User@Example.com" → "user@example.com") | `Email_RoundTrip_TrimsAndLowercasesWhitespacePaddedInput` ("  JOHN@DOMAIN.COM  " → "john@domain.com") |
| `PermissionKey_RoundTrip_Produces_SameValue` ("users.read") | `PermissionKey_RoundTrip_DifferentSegments_Produces_SameValue` ("admin.create") |
| `DeletionPolicy_RoundTrip_PreservesFourBooleans` (UserDefault: T,F,F,F) | `DeletionPolicy_RoundTrip_RoleDefault_PreservesThreeTrueBooleans` (RoleDefault: T,T,T,F) |

## Files Changed (PR 1b)

### Created (Infrastructure — 3 converters)
| File | Lines | Description |
|------|-------|-------------|
| `Data/Converters/EmailConverter.cs` | 16 | Email ↔ string via Email.Create normalization |
| `Data/Converters/PermissionKeyConverter.cs` | 15 | PermissionKey ↔ string |
| `Data/Converters/DeletionPolicyConverter.cs` | 24 | DeletionPolicy ↔ JSONB via System.Text.Json |

### Created (Tests — 1 file)
| File | Lines | Description |
|------|-------|-------------|
| `Infrastructure/Data/Converters/ValueObjectConverterTests.cs` | 97 | 6 test methods: 3 VO round-trips + 3 triangulation |

### Modified (Domain — 2 files)
| File | Change | Lines |
|------|--------|-------|
| `Domain/Entities/User.cs` | Private parameterless constructor for EF Core materialization | +8 |
| `Domain/Entities/RefreshToken.cs` | Private parameterless constructor + 5 private setters (Id, TokenHash, FamilyId, ExpiresAt, CreatedAt) | +18 / −5 |

### Modified (SDD — 1 file)
| File | Change |
|------|--------|
| `openspec/changes/infrastructure-ef-core/tasks.md` | Mark 1b.1–1b.8 [x] |

### Lines Changed
- **New files**: 152 lines (3 converters + 1 test)
- **Modified files**: +26 / −13 (Domain entities + tasks.md)
- **Total additions**: ~178 lines
- **Budget**: Under 400 lines ✅

## PR 1b Verification

```
$ dotnet test apps/api
ApplicationTests:  67/67 ✅
UnitTests:        239/239 ✅
IntegrationTests:  19/19 ✅ (13 PR1a + 6 PR1b)
Total:            325/325 ✅
```

## Deviations from Design (PR 1b)

1. **CS8618 suppression added to RefreshToken**: The private parameterless constructor triggers CS8618 warnings for non-nullable reference types (Id, TokenHash). Added `#pragma warning disable/restore CS8618` matching the pattern already used in User.cs.
2. **Triangulation expanded beyond deferred test file**: The deferred `ValueObjectConverterTests.cs` had 3 test methods (1 per converter). Strict TDD required triangulation — added 3 more edge/alt case tests (6 total).

## Issues Found (PR 1b)

None. All builds and tests pass clean with zero warnings.

## Deferred Folder State

- `_pr1b_deferred/`: Files successfully restored to working tree. The deferred copies remain as reference artifacts (untracked, not committed).
- `_pr1c_deferred/`: Files successfully restored to working tree. The deferred copies remain as reference artifacts (untracked, not committed).

---

## Completed Tasks (PR 1c) — 2026-06-16

**Commit**: `ac347b0` — `feat(infrastructure): add application db context core`
**Scope**: ApplicationDbContext, AuditTimestampInterceptor, NullUserSession, DI registration, PostgresFixture + DbContext tests
**Base**: PR 1b branch
**Lines**: +327 (8 files, 0 deletions)
**Tests**: `PostgresFixture` (Testcontainers PostgreSQL 17) + `ApplicationDbContextTests` (DI resolution, EnsureCreated, NoTracking)
**Review budget**: Under 400 lines — clean chunk, no split needed
**Migration**: Deferred (1c.9) — requires entity configs from Phase 2–3

| Task | Description | Status |
|------|-------------|--------|
| 1c.1 | RESTORE: `_pr1c_deferred/` files to working tree | ✅ |
| 1c.2 | RED: PostgresFixture + ApplicationDbContextTests | ✅ |
| 1c.3 | GREEN: Testcontainers.PostgreSql package added | ✅ |
| 1c.4 | GREEN: AuditTimestampInterceptor (IClock → CreatedAt/UpdatedAt) | ✅ |
| 1c.5 | GREEN: NullUserSession (scaffold for security slice) | ✅ |
| 1c.6 | GREEN: ApplicationDbContext (NoTracking, SplitQuery, 7 DbSets) | ✅ |
| 1c.7 | GREEN: DependencyInjection.cs (AddInfrastructure extension) | ✅ |
| 1c.8 | VERIFY: dotnet test passes (Docker required) | ✅ |
| 1c.9 | DEFERRED: EF migrations | ⏸️ |

### Key Files
- `Data/ApplicationDbContext.cs` — NoTracking, SplitQuery, NpgsqlRetryingExecutionStrategy
- `Data/Interceptors/AuditTimestampInterceptor.cs` — SaveChangesInterceptor
- `Security/NullUserSession.cs` — IUserSession scaffold
- `DependencyInjection.cs` — `AddInfrastructure(IConfiguration, IClock?)`
- `Infrastructure/PostgresFixture.cs` — Testcontainers PostgreSQL 17
- `Infrastructure/ApplicationDbContextTests.cs` — DI + EnsureCreated + NoTracking

---

## Completed Tasks (PR 2) — 2026-06-16

**Commit**: `5fcf096` — `feat(infrastructure): add foundation entity configurations`
**Scope**: UserConfiguration, RoleConfiguration, PermissionConfiguration + round-trip integration tests
**Base**: PR 1c branch
**Lines**: +397 (5 files, 0 deletions)
**Tests**: `FoundationConfigurationsTests.cs` — 296 lines of CRUD, Email normalization, unique constraint, soft-delete round-trips
**Review budget**: Under 400 lines ✅
**Migration**: Deferred (2.5) — pending entity config consolidation

| Task | Description | Status |
|------|-------------|--------|
| 2.1 | RED: User/Role/Permission round-trip integration tests | ✅ |
| 2.2 | GREEN: UserConfiguration (users, PK UserId, unique Email, soft-delete) | ✅ |
| 2.3 | GREEN: RoleConfiguration (roles, PK RoleId, unique Name, soft-delete) | ✅ |
| 2.4 | GREEN: PermissionConfiguration (permissions, PK PermissionId, unique Key, no soft-delete) | ✅ |
| 2.5 | EF migrations — deferred | ⏸️ |

### Key Files
- `Data/Configurations/UserConfiguration.cs` — `HasQueryFilter(e => !e.IsDeleted)`
- `Data/Configurations/RoleConfiguration.cs` — unique Name index
- `Data/Configurations/PermissionConfiguration.cs` — unique Key index
- `Infrastructure/Data/Configurations/FoundationConfigurationsTests.cs` — 296 lines

---

## Completed Tasks (PR 3) — 2026-06-16

**Commit**: `cc8d393` — `feat(infrastructure): add relation entity configurations`
**Scope**: UserRole, RolePermission, MenuItem, RefreshToken configs + composite-key/self-ref FK tests
**Base**: PR 2 branch
**Lines**: +393 / −4 (10 files)
**Tests**: `RelationConfigurationsTests.cs` — 209 lines: composite-key uniqueness, self-ref FK, TokenHash index
**Review budget**: Under 400 lines ✅
**Migration**: Deferred (3.6)

| Task | Description | Status |
|------|-------------|--------|
| 3.1 | RED: Composite-key uniqueness, self-ref FK, TokenHash index tests | ✅ |
| 3.2 | GREEN: UserRoleConfiguration (user_roles, composite PK, FK→User/Role) | ✅ |
| 3.3 | GREEN: RolePermissionConfiguration (role_permissions, composite PK, FK→Role/Permission) | ✅ |
| 3.4 | GREEN: MenuItemConfiguration (menu_items, self-ref FK ParentId, soft-delete) | ✅ |
| 3.5 | GREEN: RefreshTokenConfiguration (refresh_tokens, unique TokenHash, FamilyId, ExpiresAt indexes) | ✅ |
| 3.6 | EF migrations — deferred | ⏸️ |

### Key Files
- `Data/Configurations/UserRoleConfiguration.cs` — composite PK {UserId, RoleId}
- `Data/Configurations/RolePermissionConfiguration.cs` — composite PK {RoleId, PermissionId}
- `Data/Configurations/MenuItemConfiguration.cs` — self-ref FK + soft-delete
- `Data/Configurations/RefreshTokenConfiguration.cs` — unique TokenHash, FamilyId, ExpiresAt indexes
- Domain modifications: private constructors/setters on UserRole, RolePermission, MenuItem for EF materialization
- `Infrastructure/Data/Configurations/RelationConfigurationsTests.cs` — 209 lines

---

## Completed Tasks (PR 4a) — 2026-06-16

**Commit**: `3daff9c` — `feat(infrastructure): add base repository core`
**Scope**: BaseRepository<TEntity, TId> + UserRepository + IBaseRepository contract + BaseRepositoryTests
**Base**: PR 3 branch
**Lines**: +381 / −19 (5 files)
**Tests**: `BaseRepositoryTests.cs` — 206 lines: GetByIdAsync (includeDeleted true/false), AddAsync, Update, Delete, GetPagedAsync
**Review budget**: Under 400 lines ✅

| Task | Description | Status |
|------|-------------|--------|
| 4.1 | RED: BaseRepository tests (CRUD + paged + includeDeleted) | ✅ |
| 4.3 | GREEN: BaseRepository<TEntity, TId> (IgnoreQueryFilters, Skip/Take) | ✅ |
| 4.4 | GREEN: UserRepository (GetByEmailAsync, ExistsAsync, GetActiveSuperadminsAsync) | ✅ |

### Key Files
- `Data/Repositories/BaseRepository.cs` — 101 lines, generic CRUD + paging
- `Data/Repositories/UserRepository.cs` — 51 lines, email-lookup + admin queries
- `Abstractions/Persistence/IBaseRepository.cs` — updated contract
- `Infrastructure/Data/Repositories/BaseRepositoryTests.cs` — 206 lines

---

## Completed Tasks (PR 4b) — 2026-06-16

**Commit**: `bdd84c2` — `feat(infrastructure): add user role repositories`
**Scope**: RoleRepository + UserRepositoryTests + RoleRepositoryTests + DI wiring
**Base**: PR 4a branch
**Lines**: +362 (5 files)
**Tests**: `UserRepositoryTests.cs` (169 lines) + `RoleRepositoryTests.cs` (119 lines) + `BaseRepositoryTests.cs` extended (+36 lines)
**Review budget**: Under 400 lines ✅

| Task | Description | Status |
|------|-------------|--------|
| 4.2 | RED: UserRepository tests (GetByEmailAsync, ExistsAsync, GetActiveSuperadminsAsync) | ✅ |
| 4.5 | GREEN: RoleRepository (role-specific queries) | ✅ |

### Key Files
- `Data/Repositories/RoleRepository.cs` — 32 lines
- `DependencyInjection.cs` — DI registration for UserRepository + RoleRepository
- `Infrastructure/Data/Repositories/UserRepositoryTests.cs` — 169 lines
- `Infrastructure/Data/Repositories/RoleRepositoryTests.cs` — 119 lines

---

## Completed Tasks (PR 5) — 2026-06-16

**Scope**: Remaining three repositories (Permission, RefreshToken, MenuItem) + integration tests + DI registration
**Base**: PR 4b branch (`feature/infrastructure-ef-core-pr4b`)
**Lines**: +400 (7 files: 6 new, 1 modified), 0 deletions
**Review budget**: Exactly 400 changed lines ✅ (397 new + 3 DI additions)
**Tests**: 13 new integration tests across three test classes, all against PostgreSQL 17 via Testcontainers

| Task | Description | Status |
|------|-------------|--------|
| 5.1 | RED: PermissionRepository, RefreshTokenRepository, MenuItemRepository integration tests | ✅ |
| 5.2 | GREEN: PermissionRepository — GetByKeyAsync, ExistsByKeyAsync | ✅ |
| 5.3 | GREEN: RefreshTokenRepository — GetByTokenHashAsync, GetActiveByFamilyIdAsync, RevokeFamilyAsync (ExecuteUpdate) | ✅ |
| 5.4 | GREEN: MenuItemRepository — GetAllAsync, GetChildrenAsync | ✅ |

## TDD Cycle Evidence (PR 5)

| Task | Test File | Layer | RED | GREEN | REFACTOR |
|------|-----------|-------|-----|-------|----------|
| 5.1 | `PermissionRepositoryTests.cs` (4 tests) | Integration (PostgreSQL) | ✅ 16× CS0246 (missing repo types) | ✅ 4/4 pass | ✅ Trimmed to budget |
| 5.1 | `RefreshTokenRepositoryTests.cs` (6 tests) | Integration (PostgreSQL) | ✅ 16× CS0246 (missing repo types) | ✅ 6/6 pass | ✅ Trimmed to budget |
| 5.1 | `MenuItemRepositoryTests.cs` (3 tests) | Integration (PostgreSQL) | ✅ 16× CS0246 (missing repo types) | ✅ 3/3 pass | ✅ Trimmed to budget |
| 5.2 | `PermissionRepository.cs` | Infrastructure | N/A (new) | ✅ All tests pass | ✅ Clean |
| 5.3 | `RefreshTokenRepository.cs` | Infrastructure | N/A (new) | ✅ All tests pass | ✅ Clean |
| 5.4 | `MenuItemRepository.cs` | Infrastructure | N/A (new) | ✅ All tests pass | ✅ Clean |

### Test Summary (PR 5)
- **New tests written**: 13 (4 Permission + 6 RefreshToken + 3 MenuItem)
- **Tests passing**: 13/13 via `dotnet test` (Docker required for PostgreSQL)
- **Full suite**: 379/379 (67 Application + 239 Unit + 73 Integration)
- **Layers used**: Integration (13, PostgreSQL Testcontainers)

### Files Changed (PR 5)

#### Created (Infrastructure — 3 repositories)
| File | Lines | Description |
|------|-------|-------------|
| `Data/Repositories/PermissionRepository.cs` | 31 | GetByKeyAsync, ExistsByKeyAsync |
| `Data/Repositories/RefreshTokenRepository.cs` | 46 | GetByTokenHashAsync, GetActiveByFamilyIdAsync, RevokeFamilyAsync (ExecuteUpdateAsync) |
| `Data/Repositories/MenuItemRepository.cs` | 35 | GetAllAsync, GetChildrenAsync |

#### Created (Tests — 3 files)
| File | Lines | Description |
|------|-------|-------------|
| `Infrastructure/Data/Repositories/PermissionRepositoryTests.cs` | 78 | 4 tests: key lookup + existence |
| `Infrastructure/Data/Repositories/RefreshTokenRepositoryTests.cs` | 122 | 6 tests: hash lookup, active-family filtering, family revocation |
| `Infrastructure/Data/Repositories/MenuItemRepositoryTests.cs` | 85 | 3 tests: full hierarchy, direct children, empty children |

#### Modified (1 file)
| File | Change |
|------|--------|
| `DependencyInjection.cs` | +3 lines: register IPermissionRepository, IRefreshTokenRepository, IMenuItemRepository |

### Deviations from Design

1. **`MenuItemRepository.GetAllAsync` is `GetRootItemsAsync`-equivalent**: The design listed `GetRootItemsAsync` but the `IMenuItemRepository` contract specifies `GetAllAsync` (returns complete hierarchy for cycle detection per `MenuItem.SetParent`). Implemented per the actual contract, not the task label.
2. **Permissive `ParentId` comparison in `GetChildrenAsync`**: Uses `.Equals()` for `MenuItemId?` comparison, which handles null gracefully. The query filter already excludes soft-deleted items so no `includeDeleted` parameter is needed on `GetAllAsync`/`GetChildrenAsync`.

### Issues Found

None. All builds and tests pass clean with zero warnings.

### Remaining Tasks

- [ ] 1c.9 **DEFERRED**: EF migrations (InitialSchema) — requires entity configs from all phases
- [ ] 2.5 **DEFERRED**: EF migrations (AddFoundationConfigs)
- [ ] 3.6 **DEFERRED**: EF migrations (AddRelationConfigs)

### PR 5 Verification

```
$ dotnet test apps/api
ApplicationTests:   67/67 ✅
UnitTests:         239/239 ✅
IntegrationTests:   73/73 ✅ (60 prior + 13 new PR5)
Total:             379/379 ✅

$ dotnet build apps/api/Project.slnx
0 Warnings, 0 Errors
```

### All Phases Complete

All implementation phases (1a through 5) are now complete. Deferred migrations remain as tracked in `tasks.md`. The change is ready for `sdd-verify`.
