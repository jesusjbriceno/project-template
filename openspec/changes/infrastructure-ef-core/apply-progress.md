# Apply Progress: Infrastructure EF Core — 3-Way Split

**Change**: infrastructure-ef-core
**Date**: 2026-06-15 (original), split 2-way 2026-06-15, split 3-way 2026-06-15
**Phase**: 1a — Typed ID Converters & Comparers (PR 1a)
**Status**: READY FOR REVIEW (PR 1a)

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

- [ ] PR 1b: Restore `_pr1b_deferred/` files + implement VO converters and Domain EF prep
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
