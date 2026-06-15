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

- [x] PR 1b: Restore `_pr1b_deferred/` files + implement VO converters and Domain EF prep ✅
- [ ] PR 1c: Restore `_pr1c_deferred/` files + implement DbContext core
- [ ] Phase 2: Foundation Entity Configurations (User/Role/Permission)
- [ ] Phase 3: Relation Entity Configurations (UserRole/RolePermission/MenuItem/RefreshToken)
- [ ] Phase 4: Repository Core (Base + User/Role repos)
- [ ] Phase 5: Remaining Repositories

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
- `_pr1c_deferred/`: **Untouched** — remains in safe deferred state for next PR slice. Zero contamination risk.
