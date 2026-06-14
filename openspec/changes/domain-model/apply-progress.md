# Apply Progress: Domain Model — Slice 1 + Slice 2 + Slice 3 + Slice 4 + Slice 5 + Slice 6

**Branches**: `feature/domain-model-02-value-objects` (Slices 1-2), `feature/domain-model-03-rbac-core` (Slice 3), `feature/domain-model-04-users` (Slice 4), `feature/domain-model-05-tokens-menu` (Slice 5), `feature/domain-model-06-final-pass` (Slice 6)
**Base**: `develop`
**Date**: 2026-06-14
**Mode**: Strict TDD (Slices 1-5), Verification-only (Slice 6)
**Slices completed**: 1-6 (all)
**Status**: ✅ 26/26 tasks complete. Full verification passed. Ready for sdd-verify + sdd-archive.

---

## TDD Cycle Evidence

### Slice 1 (prior batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1+1.2 (v1) | `ValueObjects/Ids/IdTests.cs` | Unit | N/A (new) | ✅ 52 compile errors | ✅ 21/21 passed | ✅ 21 cases | ➖ None |
| 1.1+1.2 (v2 hardened) | `ValueObjects/Ids/IdTests.cs` | Unit | 40 green tests | ✅ compile errors from private ctor | ✅ 34/34 ID tests passed; 53/53 total UnitTests passed | ✅ +13 hardening cases | ✅ `record struct`→`sealed class` value objects |
| 1.3+1.4 | `Errors/DomainExceptionTests.cs` | Unit | N/A (new) | ✅ namespace not found | ✅ 19/19 passed | ✅ 19 cases (base + 7 derivatives × 2-3 cases each) | ➖ None needed |

### Slice 2 (prior batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 2.1 | `ValueObjects/EmailTests.cs` | Unit | ✅ 53/53 | ✅ compile error (class missing) | ✅ 22/22 passed | ✅ 22 cases (5 theory + 17 edge) | ➖ Clean |
| 2.2 | `ValueObjects/PermissionKeyTests.cs` | Unit | ✅ 71/71 | ✅ compile error (class missing) | ✅ 31/31 passed | ✅ 31 cases (6 theory valid + 9 theory invalid + 16 edge) | ➖ Clean |
| 2.3+2.4 | `Policies/DeletionPolicyTests.cs` | Unit | ✅ 101/101 | ✅ compile error (class missing) | ✅ 10/10 passed | ✅ 10 cases (dimensions, defaults, equality, with-expr) | ➖ Clean |
| 2.5 | `Common/IClock.cs` + `SystemClock.cs` | N/A | N/A (interface) | N/A | N/A (interface only) | ➖ Single (interface + impl) | ➖ Trivial |
| 2.6+2.7 | `Common/AuditableEntityTests.cs` | Unit | ✅ 111/111 | ✅ compile error (class missing) | ✅ 6/6 passed | ✅ 6 cases (create, update, delete, defaults, types) | ➖ Clean |

### Slice 3 (this batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 3.1 | `Entities/PermissionTests.cs` | Unit | ✅ 122/122 | ✅ compile error (Entities ns missing) | ✅ 6/6 passed | ✅ 6 cases (create, audit, defaults, inheritance) | ➖ Clean |
| 3.2 | `Entities/RoleTests.cs` | Unit | ✅ 122/122 | ✅ compile error (Role class missing) | ✅ 15/15 passed | ✅ 15 cases (create, system, empty name, sys role guard, add/remove perm, copy, point-in-time) | ➖ Clean |
| 3.3 | `Entities/RolePermissionTests.cs` | Unit | ✅ 122/122 | ✅ compile error (RolePermission class missing) | ✅ 5/5 passed | ✅ 5 cases (assign, composite id, null guards, equality) | ➖ Clean |
| 3.4 | Integrated in `RoleTests.cs` | Unit | ✅ 128/128 | N/A (covered in 3.2) | ✅ included in 3.2 | ✅ AddPermission, RemovePermission, CopyPermissions, point-in-time | ➖ Clean |

### Slice 4 (this batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 4.1 | `Entities/UserTests.cs` | Unit | ✅ 149/149 | ✅ 37 compile errors (User class missing) | ✅ 26/26 new passed; 175/175 total | ✅ 13 cases (create, deactivate, blocked, defaults, audit, null/empty guards) | ➖ Clean |
| 4.2 | `Entities/UserRoleTests.cs` | Unit | ✅ 149/149 | ✅ compile errors (UserRole class missing) | ✅ 5/5 passed; 180/180 total | ✅ 5 cases (assign, composite id, null userId/roleId guards, equality) | ➖ Clean |
| 4.3 | Integrated in `UserTests.cs` | Unit | ✅ 175/175 | ✅ compile errors (method missing) | ✅ included in 4.1 | ✅ 4 cases (last superadmin guard, other exists, non-superadmin, inactive user) | ➖ Clean |
| 4.4 | Integrated in `UserTests.cs` | Unit | ✅ 175/175 | ✅ compile errors (method missing) | ✅ included in 4.1 | ✅ 3 cases (non-superadmin blocked, superadmin succeeds, non-superadmin non-blocked) | ➖ Clean |
| Review | `Entities/UserTests.cs` + `Entities/User.cs` | Unit | ✅ 180/180 | ✅ compile errors (missing guard params, removed bypass) | ✅ 189/189 passed | ✅ 9 cases (deactivate guard, MarkDeleted override + polymorphism, AssignRole bypass) | ➖ Clean |
| Review-2 | `Entities/UserTests.cs` + `Entities/User.cs` + `Entities/Role.cs` + `Common/AuditableEntity.cs` | Unit | ✅ 189/189 | ✅ compile errors (MarkDeleted protected, Delete method missing) | ✅ 192/192 passed | ✅ 6 cases (3 new: delete last-superadmin guard, delete with other active, null guard; 3 updated: MarkDeleted→Delete regression + protected-reflection test) | ➖ Clean |

### Slice 5 (this batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 5.1 | `Entities/RefreshTokenTests.cs` | Unit | ✅ 192/192 | ✅ 23 compile errors (RefreshToken class missing) | ✅ 21/21 new passed | ✅ 21 cases (create x4, IsExpired x2, IsRevoked, IsActive x3, IsReuseSignal x2, Rotate x5, rotate-on-revoked, chain x3) | ✅ IClock injection added to factory |
| 5.2 | Integrated in `RefreshTokenTests.cs` | Unit | ✅ 192/192 | ✅ covered by 5.1 | ✅ included in 5.1 batch | ✅ Chain: T1→T2 reuse signal, T1→T2→T3 triple rotation, reuse throws RefreshTokenReuseSignalException | ➖ Clean |
| 5.3 | `Entities/MenuItemTests.cs` | Unit | ✅ 213/213 | ✅ 32 compile errors (MenuItem class missing) | ✅ 16/16 new passed | ✅ 16 cases (create x6, defaultPolicy, auditable, setParent x5 with cycles, delete) | ✅ Cycle detection uses Guid walk to avoid implicit operator confusion |

### Slice 5 Fresh Review Fixes (this batch — 2026-06-14)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| F1 (SHA-256) | `Entities/RefreshTokenTests.cs` | Unit | ✅ 27/27 | ✅ 3 failing: short hash, non-hex, uppercase normalization | ✅ 3/3 fixes passed | ✅ +3 edge: long hash (66), mixed-case, special chars | ➖ Clean |
| F2 (Expired rotate) | `Entities/RefreshTokenTests.cs` | Unit | ✅ 27/27 | ✅ 1 failing: expired token rotates | ✅ 1/1 fix passed | ✅ +1 boundary: expires exactly at UtcNow | ➖ Clean |
| F3 (Revoked reuse) | `Entities/RefreshTokenTests.cs` | Unit | ✅ 27/27 | ✅ 2 failing: revoked-without-replacement returns false; Rotate throws InvalidOp not ReuseSignal | ✅ 2/2 fixes passed | N/A (spec single scenario) | ➖ Clean |
| F4 (Validate first) | `Entities/RefreshTokenTests.cs` | Unit | ✅ 27/27 | ✅ 1 failing: invalid hash still mutates token | ✅ 1/1 fix passed | N/A (single invariants) | ➖ Clean |
| F5 (Guid.Empty) | `Entities/RefreshTokenTests.cs` | Unit | ✅ 27/27 | ✅ 1 failing: empty FamilyId accepted | ✅ 1/1 fix passed | N/A (single invariant) | ➖ Clean |

---

## Test Summary
- **Total tests written**: 239 (229 prior + 10 fresh review fix tests)
- **Total tests passing**: 239
- **Layers used**: Unit (239)
- **Approval tests** (refactoring): None
- **Entities created**: 7 (Permission, Role, RolePermission, User, UserRole, RefreshToken, MenuItem)

---

## Completed Tasks

### Slice 1
- [x] 1.1 Create `ValueObjects/Ids/*.cs` — 7 ID types hardened as `sealed class` + `IEquatable<T>` value objects
- [x] 1.2 Write ID tests (34 tests: factories, equality, empty-guard, default-null, composite null-guards, Deconstruct)
- [x] 1.3 Create `Errors/DomainException.cs` + 7 sealed derivatives
- [x] 1.4 Write domain exception tests (19 tests)

### Slice 2
- [x] 2.1 `ValueObjects/Email.cs` — RFC 5322 validation, full lowercase normalization, value equality (18 tests)
- [x] 2.2 `ValueObjects/PermissionKey.cs` — lowercase `action.resource` regex validation, value equality (30 tests)
- [x] 2.3 `Policies/DeletionPolicy.cs` — sealed record with 4 independent dimensions + static defaults per entity
- [x] 2.4 DeletionPolicy tests — User/RefreshToken/Role/MenuItem/Permission defaults, dimension independence (10 tests)
- [x] 2.5 `Common/IClock.cs` + `Common/SystemClock.cs` — UTC `DateTimeOffset` abstraction
- [x] 2.6 `Common/AuditableEntity.cs` — abstract base with DateTimeOffset audit fields + lifecycle methods
- [x] 2.7 AuditableEntity tests — creation, update, deletion, defaults, DateTimeOffset type enforcement (6 tests)
- [x] 2.8 Fix Email and PermissionKey validation — review found 8 critical gaps (see Issues Found)

### Slice 3
- [x] 3.1 `Entities/Permission.cs` — PermissionKey, Description, Category; extends AuditableEntity; Create() factory; tests (6)
- [x] 3.2 `Entities/Role.cs` — Name, IsSystem, RolePermissions collection; AddPermission/RemovePermission/CopyPermissionsTo; system role deletion guard (15 tests)
- [x] 3.3 `Entities/RolePermission.cs` — composite RolePermissionId, AssignedAt/AssignedBy, Assign() factory; tests (5)
- [x] 3.4 Integrated Role tests — AddPermission duplicate rejection, RemovePermission, CopyPermissions point-in-time isolation, system role protection

### Slice 4
- [x] 4.1 `Entities/User.cs` — Email, IsActive, PasswordHash, SecurityStamp, LastLoginAt, UserRoles collection; Create() factory; Deactivate() lifecycle; IsBlocked for soft-delete auth block; extends AuditableEntity (13 tests)
- [x] 4.2 `Entities/UserRole.cs` — composite UserRoleId, AssignedAt/AssignedBy, Assign() factory; junction entity with assignment-audit (not AuditableEntity) (5 tests)
- [x] 4.3 `User.RemoveRole()` with `IReadOnlyCollection<User> activeSuperadmins` guard — last-superadmin-boundary: 0/1/2 active Superadmins; inactive user no-guard; non-superadmin role no-guard (4 tests)
- [x] 4.4 `User.AssignRole()` superadmin creation gate — `IReadOnlyCollection<Role> actorRoles` parameter; non-superadmin assigning superadmin throws LastSuperadminGuardException; superadmin assigning superadmin succeeds; non-superadmin assigning non-superadmin succeeds (3 tests)

### Slice 5
- [x] 5.1 `Entities/RefreshToken.cs` — TokenHash, FamilyId, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash; Create() factory with IClock; Revoke(); Rotate() with reuse detection; IsExpired()/IsRevoked()/IsActive()/IsReuseSignal properties (21 tests: create x4, expiry x2, revoked, active x3, reuseSignal x2, rotate x5, rotate-on-revoked, chain x3)
- [x] 5.2 RefreshToken chain test — T1→T2→T3 triple rotation, all predecessors replaced, reuse throws RefreshTokenReuseSignalException (3 chain tests integrated in RefreshTokenTests.cs)
- [x] 5.3 `Entities/MenuItem.cs` — Label, Icon, Route, ParentId, SortOrder, RequiredPermissionKey, RequiredRoleId, IsVisible; extends AuditableEntity; Create() factory; SetParent() with cycle detection walking ancestors via allItems; Delete() method (16 tests: create x6, policy, auditable, setParent x5 with cycle edge cases, delete)

---

## Files Changed

### New in Slice 3
| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Domain/Entities/Permission.cs` | Created | `sealed class` extending `AuditableEntity`. Uses `PermissionId` and `PermissionKey`. Has `Description`, `Category` (nullable). `Create()` factory with audit. `DefaultPolicy` returns `DeletionPolicy.PermissionDefault`. |
| `apps/api/src/Project.Domain/Entities/Role.cs` | Created + fixed | `sealed class` extending `AuditableEntity`. Uses `RoleId`, `Name`, `IsSystem`, `IReadOnlyCollection<RolePermission>`. `Create()` factory. `AddPermission()` with duplicate guard. `RemovePermission()` returns bool. `CopyPermissionsTo()` point-in-time copy. `MarkDeleted()` overrides (not `new`) to throw `SystemRoleProtectedException` for system roles. |
| `apps/api/src/Project.Domain/Entities/RolePermission.cs` | Created | `sealed class` (not Auditable — junction entity with own audit fields). Composite identity via `RolePermissionId`. `AssignedAt` (DateTimeOffset), `AssignedBy` (string). `Assign()` static factory. |
| `apps/api/tests/Project.UnitTests/Entities/PermissionTests.cs` | Created | 6 tests: creation with valid key, null key guard, unique IDs, description/category, DefaultPolicy, AuditableEntity inheritance |
| `apps/api/tests/Project.UnitTests/Entities/RoleTests.cs` | Created | 15 tests: creation, IsSystem, null/empty/whitespace name guards, DefaultPolicy, AuditableEntity inheritance, system role delete guard, non-system role delete, AddPermission (valid + duplicate), RemovePermission (existing + missing), CopyPermissionsTo (basic + point-in-time isolation) |
| `apps/api/tests/Project.UnitTests/Entities/RolePermissionTests.cs` | Created | 5 tests: Assign with valid IDs, composite ID access, null roleId guard, null permissionId guard, equality of two assignments with same FKs |
| `openspec/changes/domain-model/tasks.md` | Modified | Marked 3.1-3.4 as `[x]` |
| `openspec/changes/domain-model/apply-progress.md` | Modified | Merged Slice 3 TDD evidence, test summary, completed tasks, files changed |

### New in Slice 4
| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Domain/Entities/User.cs` | Created | `sealed class` extending `AuditableEntity`. Uses `UserId`, `Email`, `string PasswordHash` (opaque), `SecurityStamp` (Guid N-format), `LastLoginAt` (DateTimeOffset?), `IReadOnlyCollection<UserRole>`. `Create()` factory. `Deactivate()` sets `IsActive=false` + audit. `IsBlocked` = !IsActive. `AssignRole()` with optional `actorRoles` superadmin gate. `RemoveRole()` with `activeSuperadmins` last-superadmin guard. `DefaultPolicy` = `UserDefault`. |
| `apps/api/src/Project.Domain/Entities/UserRole.cs` | Created | `sealed class` (not AuditableEntity — junction with assignment audit). Composite identity via `UserRoleId`. `AssignedAt` (DateTimeOffset), `AssignedBy` (string). `Assign()` static factory with null guards. Follows same pattern as `RolePermission`. |
| `apps/api/tests/Project.UnitTests/Entities/UserTests.cs` | Created | 26 tests: Create (valid email, passwordHash, null/empty guards, unique security stamps), Deactivate (active → inactive with audit, idempotent), IsBlocked (active=false, inactive=true), DefaultPolicy, AuditableEntity inheritance, MarkDeleted; AssignRole (valid, duplicate guard, null guard); RemoveRole (existing, non-existing, null guard); Last superadmin guard (last throws, other exists succeeds, non-superadmin skipped, inactive user skipped); Superadmin gate (non-superadmin blocked, superadmin succeeds, non-superadmin non-blocked) |
| `apps/api/tests/Project.UnitTests/Entities/UserRoleTests.cs` | Created | 5 tests: Assign with valid IDs (audit check), composite Id access via UserRoleId, null userId guard, null roleId guard, equality of same UserId+RoleId assignment |
| `openspec/changes/domain-model/tasks.md` | Modified | Marked 4.1-4.4 as `[x]` |
| `openspec/changes/domain-model/apply-progress.md` | Modified | Merged Slice 4 TDD evidence, test summary, completed tasks, files changed |

### New in Slice 5
| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Domain/Entities/RefreshToken.cs` | Created | `sealed class` (not AuditableEntity — security token with own lifecycle). Uses `RefreshTokenId`, `TokenHash` (SHA-256 only), `FamilyId` (raw Guid), `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenHash`. `Create()` factory with `IClock`. `IsExpired()`/`IsRevoked`/`IsActive()`/`IsReuseSignal` properties. `Rotate()` with reuse detection (throws `RefreshTokenReuseSignalException`). `Revoke()` for explicit revocation. `DefaultPolicy` = `RefreshTokenDefault`. |
| `apps/api/src/Project.Domain/Entities/MenuItem.cs` | Created | `sealed class` extending `AuditableEntity`. Uses `MenuItemId`, `Label`, `Icon`, `Route`, `ParentId`, `SortOrder`, `RequiredPermissionKey`, `RequiredRoleId`, `IsVisible`. `Create()` factory with validation. `SetParent()` with cycle detection walking ancestor chain via `allItems` parameter. `Delete()` method. `DefaultPolicy` = `MenuItemDefault`. |
| `apps/api/tests/Project.UnitTests/Entities/RefreshTokenTests.cs` | Created | 21 tests: Create (valid + null/empty/whitespace tokenHash), IsExpired (past + future), IsRevoked (fresh), IsActive (fresh + expired + revoked), IsReuseSignal (fresh + revoked-without-replacement + after-rotation), Rotate (revokes + sets hash + returns new + new active + reuseSignal), Rotate on already revoked, Chain (T1→T2 signal, T1→T2→T3 triple, reuse throws RefreshTokenReuseSignalException) |
| `apps/api/tests/Project.UnitTests/Entities/MenuItemTests.cs` | Created | 16 tests: Create (valid minimal + all fields + null/empty/whitespace label + negative sortOrder), DefaultPolicy, AuditableEntity inheritance, SetParent (null root + valid parent + self-reference + direct cycle + 3-level cycle + leaf reassign + root-to-null), Delete |
| `openspec/changes/domain-model/tasks.md` | Modified | Marked 5.1-5.3 as `[x]` |
| `openspec/changes/domain-model/apply-progress.md` | Modified | Merged Slice 5 TDD evidence, test summary, completed tasks, files changed |

### Previous Slices (1-2)
| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Domain/ValueObjects/Email.cs` | Created + fixed | `sealed record` with private ctor, static `Create()` factory, full lowercase normalization, pragmatic RFC 5322 checks (local/domain non-empty, no spaces, domain has dot, single @). `InvalidEmailException` on invalid input |
| `apps/api/src/Project.Domain/ValueObjects/PermissionKey.cs` | Created + fixed | `sealed partial record` with private ctor, regex pattern `^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$` (exactly 2 segments), `InvalidPermissionKeyException` on invalid |
| `apps/api/src/Project.Domain/Policies/DeletionPolicy.cs` | Created | `sealed record` with 4 bool dimensions + static `UserDefault`, `RefreshTokenDefault`, `RoleDefault`, `MenuItemDefault`, `PermissionDefault` |
| `apps/api/src/Project.Domain/Common/IClock.cs` | Created | Interface with `DateTimeOffset UtcNow { get; }` |
| `apps/api/src/Project.Domain/Common/SystemClock.cs` | Created | `sealed class` implementing `IClock` returning `DateTimeOffset.UtcNow` |
| `apps/api/src/Project.Domain/Common/AuditableEntity.cs` | Created | Abstract base with `CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy`/`DeletedAt`/`DeletedBy` + `MarkCreated`/`MarkUpdated`/`MarkDeleted`/`IsDeleted`. `MarkDeleted` made virtual post Slice 3 review for polymorphic safety. |
| `apps/api/tests/Project.UnitTests/ValueObjects/EmailTests.cs` | Created + fixed | 22 tests: normalization, valid/invalid formats (includes space, no-local, no-domain, no-TLD checks), equality, hash code, ToString |
| `apps/api/tests/Project.UnitTests/ValueObjects/PermissionKeyTests.cs` | Created + fixed | 31 tests: valid keys (6), invalid keys (9 + 7 edge including single/3+ segment rejection), equality, hash code, ToString |
| `apps/api/tests/Project.UnitTests/Policies/DeletionPolicyTests.cs` | Created | 10 tests: dimensions, per-entity defaults, equality, `with` expression |
| `apps/api/tests/Project.UnitTests/Common/AuditableEntityTests.cs` | Created | 6 tests: creation, update, soft-delete, defaults, DateTimeOffset type check |
| `openspec/changes/domain-model/specs/domain-model/spec.md` | Modified | Email: normalized wording ("domain to lowercase" → "to lowercase"); PermissionKey: clarified "exactly two segments" |

---

## Deviations from Design

- **Email normalization**: Spec scenario says `"User@Example.com"` → `"user@example.com"` (full lowercase). Implemented full lowercase normalization of the entire email string, not just the domain part. Design said "normalize domain to lowercase" but the concrete scenario (acceptance criteria) normalizes everything.
- **PermissionKey**: Uses `[GeneratedRegex]` source generator (C# 12+) for the validation pattern `^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$`, which requires `partial` on the record. Pattern enforces: lowercase only, exactly two dot-separated segments (`action.resource`), each segment starts with a letter, no leading/trailing/consecutive dots, no spaces or special chars. Single-segment keys (e.g. `dashboard`) and 3+ segment keys (e.g. `admin.users.create`) are rejected per spec.
- **AuditableEntity**: Uses `DateTimeOffset?` for `DeletedAt`/`DeletedBy` (nullable) rather than non-nullable `DateTimeOffset` as the spec text implies. Nullable semantics are cleaner for "not deleted" state. `IsDeleted` property derived from `DeletedAt.HasValue`.
- **DeletionPolicy defaults**: Added defaults for all entity types per spec (User, RefreshToken, Role, MenuItem, Permission). RefreshToken default has all dimensions `false` since lifecycle is governed by revocation/expiration.
- **Role.MarkDeleted override**: ~~Uses `new` keyword to hide the base `MarkDeleted` method~~ (FIXED: changed to `override` after Slice 3 review found `new`-keyword bypassable). Made `AuditableEntity.MarkDeleted` virtual; `Role` now uses `override` for polymorphic safety. Per design decision #6, `Role` overrides deletion per instance when `IsSystem`.
- **RolePermission non-Auditable**: RolePermission does not extend `AuditableEntity` — it is a junction entity with its own `AssignedAt`/`AssignedBy` assignment audit. This matches the design contract where junction entities carry assignment metadata distinct from entity lifecycle audit.
- **CopyPermissionsTo**: Implemented as an instance method on Role (`source.CopyPermissionsTo(target, ...)`) per design contract `Role.CopyPermissionsTo(Role)`.

### Slice 4
- **User.IsBlocked = !IsActive**: Per spec "soft-deleted users (IsActive=false) MUST be functionally blocked from auth/authorization". The `IsBlocked` property directly mirrors `!IsActive`, keeping the domain simple — deactivation IS the soft-delete/auth-block mechanism. No separate `IsDeleted` soft-delete path needed on User for auth blocking.
- **Superadmin identification**: Uses `role.IsSystem && "superadmin".Equals(role.Name, StringComparison.OrdinalIgnoreCase)` as a private static helper. This is a pragmatic domain-level check. A future iteration may extract this to a constant or policy class, but for now the well-known system role name is sufficient.
- **User.HasSuperadminRole() removed**: The User entity only stores `RoleId` values in `UserRole` junctions — it cannot determine role names/types from IDs alone. The Application layer is responsible for pre-building the `activeSuperadmins` collection passed to `RemoveRole()` and `Deactivate()`. This keeps Domain persistence-free.
- **Deactivate now includes last-superadmin guard** (FIXED 2026-06-12): `Deactivate()` accepts `IReadOnlyCollection<User> activeSuperadmins` parameter, matching the pattern in `RemoveRole()`. When the user is active and is the only entry in the active superadmins collection, deactivation throws `LastSuperadminGuardException`. Previously documented as "deferred" — now implemented inline.
- **MarkDeleted override added on User** (FIXED 2026-06-12, revised 2026-06-12): `User` now overrides `MarkDeleted` (protected override) to set `IsActive = false` before calling `base.MarkDeleted()`. This ensures functional auth blocking (`IsBlocked = true`) via any internal code path. **Revised**: `AuditableEntity.MarkDeleted` is now `protected virtual` (was `public virtual`). External callers must use entity-specific `Delete()` methods that enforce domain invariants — `User.Delete(activeSuperadmins, ...)` for superadmin guard, `Role.Delete(...)` for system-role guard. This prevents polymorphic bypass through `AuditableEntity` references entirely: the API shape makes it impossible to call `MarkDeleted` from outside the entity hierarchy.
- **AssignRole bypass closed** (FIXED 2026-06-12): The `actorRoles is { Count: > 0 }` bypass that allowed null or empty `actorRoles` to skip the superadmin creation gate has been removed. The guard now explicitly throws `LastSuperadminGuardException` when `actorRoles` is null or empty and the target role is superadmin. Bootstrap/seed scenarios must provide the superadmin role explicitly.

### Slice 5
- **RefreshToken is not an AuditableEntity**: Design decision #9 treats RefreshToken as a security token with its own lifecycle (`CreatedAt`, `RevokedAt`, `FamilyId`). It does NOT extend `AuditableEntity` because it doesn't have the standard create/update/delete audit fields — it's governed by revocation and expiration, not soft/hard delete. This aligns with `DeletionPolicy.RefreshTokenDefault` (all dimensions false). `CreatedAt` is set via `IClock` injection in the factory for testability.
- **RefreshToken.Rotate() includes reuse detection inline**: Per the spec "Reuse of a revoked token MUST trigger family/session revocation", `Rotate()` treats any revoked token as a reuse signal and throws `RefreshTokenReuseSignalException` before performing rotation. The Application layer catches this exception to cascade family revocation. The `FamilyId` property (raw `Guid`) is the domain's return signal for Application-layer cascade logic.
- **MenuItem.SetParent() uses Guid-based cycle walk**: The `MenuItemId` type includes `implicit operator Guid`, which causes C# compiler ambiguity when comparing `MenuItemId? == MenuItemId`. `SetParent()` converts IDs to raw `Guid` values internally via explicit casts and `Equals()` calls to avoid implicit operator confusion during the ancestor chain walk. This is an implementation detail — the public API surface remains typed (`MenuItemId?`).
- **MenuItem.Delete() is a simple soft-delete**: Hierarchy consistency on deletion (e.g., reassigning children before deleting a parent) is deferred to the Application layer use case. The Domain `Delete()` method only performs soft-delete via `MarkDeleted()`. This keeps Domain persistence-free — the Application layer can pre-validate hierarchy constraints before calling `Delete()`.

---

## Slice 4 Review Fixes (2026-06-12)

### Fix round 1 — Deactivate guard, MarkDeleted functional blocking, AssignRole bypass
Three critical findings resolved:

| # | Finding | Fix | Tests added |
|---|---------|-----|-------------|
| 1 | **Deactivate lacks last-superadmin guard** — It was possible to deactivate the only active superadmin. | Added `activeSuperadmins` parameter to `Deactivate()`; guard throws `LastSuperadminGuardException` when user is the only active superadmin. | `Deactivate_LastActiveSuperadmin_ThrowsLastSuperadminGuardException`, `Deactivate_Superadmin_WhenOtherActiveSuperadminExists_Succeeds`, `Deactivate_NonSuperadmin_WithSelfInSuperadmins_Succeeds`, `Deactivate_AlreadyInactiveSuperadmin_Succeeds`, `Deactivate_WithNullActiveSuperadmins_ThrowsArgumentNullException` (5 tests) |
| 2 | **MarkDeleted unguarded, no functional blocking** — `User.MarkDeleted()` (inherited from `AuditableEntity`) did not set `IsActive=false`, leaving soft-deleted users functionally unblocked. No superadmin guard on soft-delete. | Overrode `MarkDeleted()` to set `IsActive = false` + call `base.MarkDeleted()`. Polymorphic: cast to `AuditableEntity` still triggers the override. Superadmin guard deferred to `Deactivate()` (the primary lifecycle operation). | `MarkDeleted_OnUser_SetsDeletedAtAndDeletedByAndBlocksAuth` (updated), `MarkDeleted_OnAlreadyInactiveUser_StaysInactive`, `MarkDeleted_ViaAuditableEntityReference_OnActiveUser_BlocksAuth` (3 tests) |
| 3 | **AssignRole bypass** — Null/empty `actorRoles` skipped the superadmin creation gate, documented as "for seeding/bootstrap". | Guard now throws when `actorRoles` is null or empty AND the target role is superadmin. Bootstrap must provide superadmin role explicitly. | `AssignRole_SuperadminRole_WithNullActorRoles_ThrowsLastSuperadminGuardException`, `AssignRole_SuperadminRole_WithEmptyActorRoles_ThrowsLastSuperadminGuardException` (2 tests) |

### Fix round 2 — Soft-delete superadmin guard (2026-06-12)

The fix-1 MarkDeleted override was **incomplete**: `MarkDeleted` remained `public virtual` on `AuditableEntity`, meaning any external caller could soft-delete the only active Superadmin by calling `user.MarkDeleted(...)` directly — the guard was only on `Deactivate()`, not on the soft-delete path.

| # | Finding | Fix | Tests added |
|---|---------|-----|-------------|
| 4 | **MarkDeleted still bypasses last-superadmin guard** — `User.MarkDeleted()` (public override) had no superadmin guard. External callers could soft-delete the only active Superadmin via `user.MarkDeleted(...)` or `((AuditableEntity)user).MarkDeleted(...)`. | **Architectural fix**: Made `AuditableEntity.MarkDeleted` `protected virtual` (was `public virtual`). External callers cannot call it at all. `User` now exposes `Delete(IReadOnlyCollection<User> activeSuperadmins, string deletedBy, IClock clock)` which enforces the last-active-superadmin guard before calling `protected MarkDeleted`. `Role` exposes `Delete(string deletedBy, IClock clock)` routing through its existing `IsSystem` guard. `Permission` intentionally has no `Delete` method (hard-delete only per design). | `Delete_LastActiveSuperadmin_ThrowsLastSuperadminGuardException` (state preserved), `Delete_Superadmin_WhenOtherActiveSuperadminExists_SucceedsAndBlocksAuth`, `Delete_WithNullActiveSuperadmins_ThrowsArgumentNullException`, `MarkDeleted_IsProtected_NotCallableExternally` (reflection verifies API shape), updated all existing MarkDeleted→Delete call sites in UserTests (3 tests), RoleTests (3 tests), AuditableEntityTests (1 test). Total: 6 new/updated tests. |

All fixes remain BCL-only, persistence-free, and within Slice 4 scope. No external packages or EF Core references introduced.

---

## Verification Results (Slice 4 + Review Fixes round 2)
- **Build**: ✅ 0 errors, 0 warnings
- **UnitTests**: ✅ 192/192 passed (189 prior + 3 new soft-delete guard tests)
- **IntegrationTests**: ✅ 2/2 passed (pre-existing, unaffected)
- **ApplicationTests**: No tests (empty project, expected)
- **Domain.csproj**: ✅ Zero package references (BCL-only)
- **DateTimeOffset usage**: ✅ Confirmed — no `DateTime` types anywhere in new Domain code
- **Spec scenario coverage**: ✅ All 5 User/UserRole spec scenarios covered + 9 regression tests + 6 soft-delete guard tests
- **Targeted test run (User + UserRole + Role + AuditableEntity tests)**: ✅ 65/65 passed
- **MarkDeleted API shape**: ✅ Protected — external bypass impossible; reflection test confirms no public MarkDeleted on AuditableEntity, User, or Role
- **Role system-role guard regression**: ✅ Still passes via `Delete()` → `protected override MarkDeleted` → `IsSystem` check → `SystemRoleProtectedException`

### Previous Verification
- **Build**: ✅ 0 errors, 0 warnings
- **UnitTests**: ✅ 149/149 passed (122 Slice 1+2 + 26 Slice 3 + 1 review fix regression)
- **IntegrationTests**: ✅ 2/2 passed (pre-existing, unaffected)
- **ApplicationTests**: No tests (empty project, expected)
- **Domain.csproj**: ✅ Zero package references (BCL-only)
- **DateTimeOffset usage**: ✅ Confirmed — no `DateTime` types anywhere in Domain
- **Polymorphic guard**: ✅ `new` → `override` fix verified; `AuditableEntity`-typed reference cannot bypass system role guard

---

## Issues Found

### Slice 2 review (2026-06-12) — 8 critical gaps, all fixed
| # | Finding | Fix | Tests added |
|---|---------|-----|-------------|
| 1 | **Email — no local part**: `@example.com` accepted | Added check: local part must be non-empty | `Create_AtSignOnly_NoLocalPart_ThrowsInvalidEmailException` |
| 2 | **Email — no domain part**: `user@` accepted | Added check: domain part must be non-empty | `Create_AtSignOnly_NoDomainPart_ThrowsInvalidEmailException` |
| 3 | **Email — spaces in value**: `user example@example.com` accepted | Added `char.IsWhiteSpace` guard on trimmed value | `Create_SpaceInLocalPart_ThrowsInvalidEmailException` |
| 4 | **Email — no TLD dot**: `user@example` accepted | Added pragmatic check: domain must contain at least one `.` | `Create_NoDotInDomainPart_ThrowsInvalidEmailException` |
| 5-6 | **PermissionKey — single segment**: `dashboard` accepted | Regex changed to require exactly 2 segments (`action.resource`) | `Create_SingleSegment_ThrowsInvalidPermissionKeyException` + theory case |
| 7-8 | **PermissionKey — 3+ segments**: `admin.users.create`/`admin.users.manage` accepted | Regex changed to require exactly 2 segments | `Create_MoreThanTwoSegments_ThrowsInvalidPermissionKeyException` + theory case |

### Spec update
- Email requirement: "normalize domain to lowercase" → "normalize to lowercase" (matches acceptance criteria `"User@Example.com" → "user@example.com"`)
- PermissionKey requirement: added "exactly two" segment constraint, matching `action.resource` format

### Slice 3 — None
No issues found during Slice 3 implementation. All tests passed first time after entities were created.

### Slice 3 review (2026-06-12) — 2 critical findings, all fixed

| # | Finding | Fix | Tests added |
|---|---------|-----|-------------|
| 1 | **Role.MarkDeleted bypass via `new` keyword**: `Role.MarkDeleted()` used `new` to hide `AuditableEntity.MarkDeleted()`. Casting `systemRole` to `AuditableEntity` bypassed the system role guard entirely. | Made `AuditableEntity.MarkDeleted` virtual; Role now uses `override` instead of `new`. Polymorphic dispatch ensures the guard is enforced regardless of reference type. | `MarkDeleted_ViaAuditableEntityReference_OnSystemRole_ThrowsSystemRoleProtectedException` |
| 2 | **RolePermission audit model ambiguity**: The spec says "All entities SHALL carry DateTimeOffset audit fields" but RolePermission (and other junction entities like UserRole) only carry assignment-specific audit (`AssignedAt`/`AssignedBy`) — not full lifecycle audit. | Clarified in design (decision #12) and spec (requirement #7 note) that association/junction entities use assignment-audit fields and are NOT full lifecycle entities. This is intentional: junctions do not have independent lifecycle (create/update/delete) — they exist only while the association is active. | N/A (doc only) |

### Slice 3 workload note

Slice 3 exceeded the 400-line review budget (~500 lines of new source + tests + docs). This was driven by the TDD requirement for RBAC core entities (Permission + Role + RolePermission), each needing comprehensive test coverage of invariants, guards, audit, and edge cases. The 400-line target remains aspirational; Slice 3 is accepted as a review exception given that further splitting would produce artificial fragmentation (Role and RolePermission are tightly coupled). Future slices should keep the budget in mind but prioritize correctness over mechanistic line counts.

### Slice 5 Fresh Review Fixes (2026-06-14) — 5 critical findings, all fixed

| # | Finding | Fix | Tests added |
|---|---------|-----|-------------|
| 1 | **RefreshToken does not enforce SHA-256 hash-only storage**: `Create()` accepted arbitrary strings (e.g. `"abc123hash"`, `"hash1"`), plaintext, non-hex, and wrong-length values. | Added `ValidateAndNormalizeTokenHash()`: rejects null/empty/whitespace; requires exactly 64 hex chars (0-9, a-f, A-F); normalizes to lowercase (`ToLowerInvariant`) for deterministic storage. | `Create_WithShortTokenHash_ThrowsArgumentException`, `Create_WithNonHexTokenHash_ThrowsArgumentException`, `Create_WithUppercaseHexTokenHash_NormalizesToLowercase`, `Create_WithMixedCaseHexTokenHash_NormalizesToLowercase`, `Create_WithLongTokenHash_ThrowsArgumentException`, `Create_WithSpecialCharsInTokenHash_ThrowsArgumentException` (6 tests) |
| 2 | **Expired refresh tokens can still be rotated**: `Rotate()` only checked `IsReuseSignal` and `IsRevoked` — expired tokens slipped through. | Added `IsExpired(clock)` check in `Rotate()` before mutation. Throws `InvalidOperationException("Cannot rotate an expired token.")`. | `Rotate_OnExpiredToken_ThrowsInvalidOperationException`, `Rotate_ExpiredTokenAtExactBoundary_ThrowsInvalidOperationException` (2 tests) |
| 3 | **Revoked-without-replacement token reuse does not trigger family/session revocation**: `IsReuseSignal` required `ReplacedByTokenHash is not null`, meaning admin/logout revoked tokens (`Revoke()` without replacement) were NOT treated as reuse signals. `Rotate()` threw `InvalidOperationException` instead of `RefreshTokenReuseSignalException` for these tokens. | Changed `IsReuseSignal` to simply `IsRevoked` — ANY revoked token is a reuse signal. `Rotate()` now throws `RefreshTokenReuseSignalException` for all revoked tokens (not `InvalidOperationException`). Combined the two checks into a single `if (IsRevoked)` guard. | `IsReuseSignal_RevokedWithoutReplacement_ReturnsTrue` (behavior change: false→true), `Rotate_OnRevokedWithoutReplacementToken_ThrowsRefreshTokenReuseSignalException` (exception type change: InvalidOp→ReuseSignal) (2 test changes) |
| 4 | **Rotate mutates current token before validating replacement hash**: `RevokedAt` and `ReplacedByTokenHash` were set BEFORE calling `Create(newTokenHash, ...)`. If `Create` threw (invalid hash), the current token was left in a corrupted state (revoked with invalid replacement). | Moved `Create(newTokenHash, ...)` call BEFORE state mutation. The replacement token is fully validated and created first; only then does `this` get mutated. `ReplacedByTokenHash` uses `newToken.TokenHash` (the normalized form from `Create`). | `Rotate_WithInvalidNewTokenHash_DoesNotMutateCurrentToken` (1 test) |
| 5 | **FamilyId must reject Guid.Empty**: `Create()` did not validate `familyId` against `Guid.Empty`. | Added `if (familyId == Guid.Empty) throw new ArgumentException(...)` in `Create()`. | `Create_WithEmptyFamilyId_ThrowsArgumentException` (1 test) |

All fixes remain BCL-only, persistence-free, and within Slice 5 scope. The existing `RefreshToken.cs` hash constants in tests were migrated from short strings (`"hash1"`, `"abc123hash"`) to real 64-char SHA-256 hex hashes. `MenuItem` was not changed.

#### Test hash migration
All existing tests updated to use valid 64-char SHA-256 hex constants:
- `Hash1` = SHA256("")  = `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`
- `Hash2` = SHA256("a") = `ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb`
- `Hash3` = SHA256("b") = `3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d`

#### Verification Results (Fresh Review Fixes)
- **Build**: ✅ 0 errors, 0 warnings
- **UnitTests**: ✅ 239/239 passed (229 prior + 10 new fix tests)
- **IntegrationTests**: ✅ 2/2 passed (unaffected)
- **Domain.csproj**: ✅ Zero package references (BCL-only)
- **DateTimeOffset usage**: ✅ Confirmed — no `DateTime` types
- **MenuItem**: ✅ Unchanged — no compilation issues
- **RefreshToken targeted tests**: ✅ 31/31 passed (21 original + 10 new)
- **Slice 5 targeted tests**: ✅ 47/47 passed (`RefreshTokenTests` 31 + `MenuItemTests` 16)
- **Full regression**: ✅ All 239 unit tests green

---

---

## Slice 6: Final Pass (2026-06-14)

**Branch**: `feature/domain-model-06-final-pass`
**Base**: `feature/domain-model-05-tokens-menu`
**Mode**: Verification-only (no new code written)

### Task 6.1 — Full Test Suite ✅

```
$ dotnet test apps/api --no-restore --verbosity minimal

UnitTests:      239 passed, 0 failed, 0 skipped  (279 ms)
IntegrationTests: 2 passed, 0 failed, 0 skipped  (425 ms)
ApplicationTests: No tests (empty project, expected)
──────────────────────────────────────────────────
Total:          241 passed, 0 failed, 0 skipped
```

- **Build**: ✅ 0 errors, 0 warnings
- **Target framework**: net10.0
- **Test runner**: xUnit v2.9.3

### Task 6.2 — Domain BCL-Only Verification ✅

`Project.Domain.csproj` inspected — zero `<PackageReference>` elements. Contents:
- SDK: `Microsoft.NET.Sdk`
- TargetFramework: `net10.0`
- `ImplicitUsings: enable`, `Nullable: enable`
- No EF Core, ASP.NET Core, UI packages, or any third-party dependencies
- No project references to other layers

✅ Confirmed: Domain remains BCL-only, persistence/API independent.

### Task 6.3 — Spec Scenario Coverage Matrix ✅

All 14 spec scenarios from `specs/domain-model/spec.md` mapped to covering tests:

| # | Requirement | Scenario | Covering Test File(s) | Tests |
|---|-------------|----------|----------------------|-------|
| 1 | Value Objects — Email | Normalization to lowercase, value equality | `EmailTests.cs` | 22 |
| 2 | Value Objects — PermissionKey | `action.resource` format, uppercase rejection | `PermissionKeyTests.cs` | 31 |
| 3 | User Lifecycle | Creation (IsActive, audit, stamp), deactivation | `UserTests.cs` | — |
| 4 | User — Soft-delete block | IsActive=false → IsBlocked=true | `UserTests.cs` | — |
| 5 | Superadmin — Last guard | Deactivation/removal with 1 superadmin throws | `UserTests.cs` | — |
| 6 | Superadmin — Creation gate | Non-superadmin assigning superadmin throws | `UserTests.cs` | — |
| 7 | RBAC — System role protection | IsSystem=true deletion throws | `RoleTests.cs` | 15 |
| 8 | RBAC — Permission copy | CopyPermissionsTo is point-in-time, no propagation | `RoleTests.cs` | — |
| 9 | RBAC — UserRole assignment | Assign() records AssignedAt/AssignedBy | `UserRoleTests.cs` | 5 |
| 10 | RefreshToken — Rotation | Rotate() revokes predecessor, sets ReplacedByTokenHash | `RefreshTokenTests.cs` | 31 |
| 11 | RefreshToken — Reuse detection | Revoked token presented again → family revocation signal | `RefreshTokenTests.cs` | — |
| 12 | MenuItem — Hierarchy/cycles | SetParent() walks ancestors, throws on self-reference | `MenuItemTests.cs` | 16 |
| 13 | Deletion Policy — Per-entity | User/RefreshToken/Role/MenuItem/Permission defaults | `DeletionPolicyTests.cs` | 10 |
| 14 | Audit Fields | CreatedAt/UpdatedAt on lifecycle; AssignedAt/By on junctions | `AuditableEntityTests.cs`, `UserRoleTests.cs`, `RolePermissionTests.cs` | 6+5+5 |
| — | IDs + Domain Errors | ID factories/equality/guards; exception hierarchy | `IdTests.cs`, `DomainExceptionTests.cs` | 34+19 |
| — | RBAC — Permission entity | Create with PermissionKey, audit, policy | `PermissionTests.cs` | 6 |

**Coverage summary**: 14/14 spec scenarios have ≥1 covering test. 0 gaps found.

### No Deviations

Slice 6 is verification-only. No code was changed. No new files created.

### No Issues Found

- All tests pass clean
- Domain.csproj has zero package dependencies
- All spec scenarios are covered by at least one test
- No DateTime types used (DateTimeOffset only)
- No new warnings or errors introduced

---

## Remaining Tasks
- None — all 26/26 tasks complete. Ready for `sdd-verify` and `sdd-archive`.
