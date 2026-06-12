# Apply Progress: Domain Model — Slice 1

**Branch**: `feature/domain-model-01-ids-errors`
**Base**: `develop`
**Date**: 2026-06-12
**Mode**: Strict TDD
**Slice**: 1 — ID primitives + domain errors
**Review budget**: ~300 lines (within 400-line limit)

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1+1.2 (v1) | `ValueObjects/Ids/IdTests.cs` | Unit | N/A (new) | ✅ 52 compile errors | ✅ 21/21 passed | ✅ 21 cases | ➖ None |
| 1.1+1.2 (v2 hardened) | `ValueObjects/Ids/IdTests.cs` | Unit | 40 green tests | ✅ compile errors from private ctor | ✅ 34/34 ID tests passed; 53/53 total UnitTests passed | ✅ +13 hardening cases | ✅ `record struct`→`sealed class` value objects |
| 1.3+1.4 | `Errors/DomainExceptionTests.cs` | Unit | N/A (new) | ✅ namespace not found | ✅ 19/19 passed | ✅ 19 cases (base + 7 derivatives × 2-3 cases each) | ➖ None needed |

## Test Summary
- **Total tests written**: 53 (34 ID + 19 error)
- **Total tests passing**: 53
- **Layers used**: Unit (53)
- **Approval tests** (refactoring): None
- **Pure functions created**: 0 (structural types)

## Completed Tasks
- [x] 1.1 Create `ValueObjects/Ids/*.cs` — 7 ID types hardened as `sealed class` + `IEquatable<T>` value objects
- [x] 1.1b Domain hardening: prevent invalid construction via `default(T)`, `new(Guid.Empty)`, and public positional constructors
- [x] 1.2 Write ID tests (34 tests: factories, equality, empty-guard, default-null, composite null-guards, Deconstruct)
- [x] 1.3 Create `Errors/DomainException.cs` + 7 sealed derivatives
- [x] 1.4 Write domain exception tests (19 tests)

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Domain/ValueObjects/Ids/UserId.cs` | **Rewritten** | `readonly record struct` → `sealed class : IEquatable<UserId>` with private ctor, `New()`/`From(Guid)`, implicit Guid conversion, `Equals`/`GetHashCode`/`==`/`!=` |
| `apps/api/src/Project.Domain/ValueObjects/Ids/RoleId.cs` | **Rewritten** | Same hardened pattern as UserId |
| `apps/api/src/Project.Domain/ValueObjects/Ids/PermissionId.cs` | **Rewritten** | Same hardened pattern as UserId |
| `apps/api/src/Project.Domain/ValueObjects/Ids/RefreshTokenId.cs` | **Rewritten** | Same hardened pattern as UserId |
| `apps/api/src/Project.Domain/ValueObjects/Ids/MenuItemId.cs` | **Rewritten** | Same hardened pattern as UserId |
| `apps/api/src/Project.Domain/ValueObjects/Ids/UserRoleId.cs` | **Rewritten** | `readonly record struct` → `sealed class : IEquatable<UserRoleId>` with private ctor, `From(UserId, RoleId)` factory, null guards, `Deconstruct` |
| `apps/api/src/Project.Domain/ValueObjects/Ids/RolePermissionId.cs` | **Rewritten** | `readonly record struct` → `sealed class : IEquatable<RolePermissionId>` with private ctor, `From(RoleId, PermissionId)` factory, null guards, `Deconstruct` |
| `apps/api/src/Project.Domain/Errors/DomainException.cs` | Created | Abstract base class inheriting Exception |
| `apps/api/src/Project.Domain/Errors/LastSuperadminGuardException.cs` | Created | Sealed derivative |
| `apps/api/src/Project.Domain/Errors/SystemRoleProtectedException.cs` | Created | Sealed derivative |
| `apps/api/src/Project.Domain/Errors/MenuCycleDetectedException.cs` | Created | Sealed derivative |
| `apps/api/src/Project.Domain/Errors/RefreshTokenReuseSignalException.cs` | Created | Sealed derivative |
| `apps/api/src/Project.Domain/Errors/InvalidEmailException.cs` | Created | Sealed derivative |
| `apps/api/src/Project.Domain/Errors/InvalidPermissionKeyException.cs` | Created | Sealed derivative |
| `apps/api/src/Project.Domain/Errors/DeletionPolicyViolationException.cs` | Created | Sealed derivative |
| `apps/api/tests/Project.UnitTests/ValueObjects/Ids/IdTests.cs` | **Rewritten** | 34 tests: +5 default-null, +4 composite null-guards, +2 default-null, +2 Deconstruct; `new`→`From()` for composites |
| `apps/api/tests/Project.UnitTests/Errors/DomainExceptionTests.cs` | Created | 19 tests: instantiation, message, inheritance for base + 7 derivatives |
| `openspec/changes/domain-model/tasks.md` | Modified | Marked 1.1-1.4 as [x] |

## Deviations from Design
- **Hardened ID types**: Original design used `readonly record struct` which allows `default(UserId)` = invalid ID and `new UserId(Guid.Empty)` bypassing the factory guard. Rewrote all 7 ID types as `sealed class` + `IEquatable<T>` value objects with private constructors, `New()`/`From()` factories, and value equality operators. Composite IDs add `From()` factory with null guards and `Deconstruct()` for destructuring. Domain BCL-only constraint preserved.
- **Design doc updated**: Decision #5 and Contracts section now reflect the hardened choice.

## Verification Results
- **Build**: ✅ 0 errors, 0 warnings
- **UnitTests**: ✅ 53/53 passed
- **IntegrationTests**: ✅ 2/2 passed (pre-existing, unaffected)
- **ApplicationTests**: No tests yet (empty project, expected)

## Issues Found
None.

## Remaining Tasks (Slice 2+)
- [ ] 2.1-2.7 Value Objects, Policies, Clock, AuditableEntity
- [ ] 3.1-3.4 Core RBAC Entities
- [ ] 4.1-4.4 User & UserRole
- [ ] 5.1-5.3 RefreshToken & MenuItem
- [ ] 6.1-6.3 Final Pass
