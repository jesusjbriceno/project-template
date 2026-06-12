# Apply Progress: Domain Model — Slice 1 + Slice 2

**Branch**: `feature/domain-model-02-value-objects`
**Base**: `develop`
**Date**: 2026-06-12
**Mode**: Strict TDD
**Slices completed**: 1 (ID primitives + domain errors), 2 (Value objects, policies, clock, auditable base)
**Status**: Slice 2 reviewed — critical findings fixed (see Issues Found)

---

## TDD Cycle Evidence

### Slice 1 (prior batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1+1.2 (v1) | `ValueObjects/Ids/IdTests.cs` | Unit | N/A (new) | ✅ 52 compile errors | ✅ 21/21 passed | ✅ 21 cases | ➖ None |
| 1.1+1.2 (v2 hardened) | `ValueObjects/Ids/IdTests.cs` | Unit | 40 green tests | ✅ compile errors from private ctor | ✅ 34/34 ID tests passed; 53/53 total UnitTests passed | ✅ +13 hardening cases | ✅ `record struct`→`sealed class` value objects |
| 1.3+1.4 | `Errors/DomainExceptionTests.cs` | Unit | N/A (new) | ✅ namespace not found | ✅ 19/19 passed | ✅ 19 cases (base + 7 derivatives × 2-3 cases each) | ➖ None needed |

### Slice 2 (this batch)
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 2.1 | `ValueObjects/EmailTests.cs` | Unit | ✅ 53/53 | ✅ compile error (class missing) | ✅ 22/22 passed | ✅ 22 cases (5 theory + 17 edge) | ➖ Clean |
| 2.2 | `ValueObjects/PermissionKeyTests.cs` | Unit | ✅ 71/71 | ✅ compile error (class missing) | ✅ 31/31 passed | ✅ 31 cases (6 theory valid + 9 theory invalid + 16 edge) | ➖ Clean |
| 2.3+2.4 | `Policies/DeletionPolicyTests.cs` | Unit | ✅ 101/101 | ✅ compile error (class missing) | ✅ 10/10 passed | ✅ 10 cases (dimensions, defaults, equality, with-expr) | ➖ Clean |
| 2.5 | `Common/IClock.cs` + `SystemClock.cs` | N/A | N/A (interface) | N/A | N/A (interface only) | ➖ Single (interface + impl) | ➖ Trivial |
| 2.6+2.7 | `Common/AuditableEntityTests.cs` | Unit | ✅ 111/111 | ✅ compile error (class missing) | ✅ 6/6 passed | ✅ 6 cases (create, update, delete, defaults, types) | ➖ Clean |

---

## Test Summary
- **Total tests written**: 122 (53 Slice 1 + 69 Slice 2)
- **Total tests passing**: 122
- **Layers used**: Unit (122)
- **Approval tests** (refactoring): None
- **Pure functions created**: 2 (Email.Create, PermissionKey.Create)

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

---

## Files Changed

| File | Action | What Was Done |
|------|--------|---------------|
| `apps/api/src/Project.Domain/ValueObjects/Email.cs` | Created + fixed | `sealed record` with private ctor, static `Create()` factory, full lowercase normalization, pragmatic RFC 5322 checks (local/domain non-empty, no spaces, domain has dot, single @). `InvalidEmailException` on invalid input |
| `apps/api/src/Project.Domain/ValueObjects/PermissionKey.cs` | Created + fixed | `sealed partial record` with private ctor, regex pattern `^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$` (exactly 2 segments), `InvalidPermissionKeyException` on invalid |
| `apps/api/src/Project.Domain/Policies/DeletionPolicy.cs` | Created | `sealed record` with 4 bool dimensions + static `UserDefault`, `RefreshTokenDefault`, `RoleDefault`, `MenuItemDefault`, `PermissionDefault` |
| `apps/api/src/Project.Domain/Common/IClock.cs` | Created | Interface with `DateTimeOffset UtcNow { get; }` |
| `apps/api/src/Project.Domain/Common/SystemClock.cs` | Created | `sealed class` implementing `IClock` returning `DateTimeOffset.UtcNow` |
| `apps/api/src/Project.Domain/Common/AuditableEntity.cs` | Created | Abstract base with `CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy`/`DeletedAt`/`DeletedBy` + `MarkCreated`/`MarkUpdated`/`MarkDeleted`/`IsDeleted` |
| `apps/api/tests/Project.UnitTests/ValueObjects/EmailTests.cs` | Created + fixed | 22 tests: normalization, valid/invalid formats (includes space, no-local, no-domain, no-TLD checks), equality, hash code, ToString |
| `apps/api/tests/Project.UnitTests/ValueObjects/PermissionKeyTests.cs` | Created + fixed | 31 tests: valid keys (6), invalid keys (9 + 7 edge including single/3+ segment rejection), equality, hash code, ToString |
| `apps/api/tests/Project.UnitTests/Policies/DeletionPolicyTests.cs` | Created | 10 tests: dimensions, per-entity defaults, equality, `with` expression |
| `apps/api/tests/Project.UnitTests/Common/AuditableEntityTests.cs` | Created | 6 tests: creation, update, soft-delete, defaults, DateTimeOffset type check |
| `openspec/changes/domain-model/tasks.md` | Modified | Marked 2.1-2.7, 2.8 as `[x]` |
| `openspec/changes/domain-model/specs/domain-model/spec.md` | Modified | Email: normalized wording ("domain to lowercase" → "to lowercase"); PermissionKey: clarified "exactly two segments" |
| `openspec/changes/domain-model/apply-progress.md` | Modified | Recorded Slice 2 review findings and fixes |

---

## Deviations from Design

- **Email normalization**: Spec scenario says `"User@Example.com"` → `"user@example.com"` (full lowercase). Implemented full lowercase normalization of the entire email string, not just the domain part. Design said "normalize domain to lowercase" but the concrete scenario (acceptance criteria) normalizes everything.
- **PermissionKey**: Uses `[GeneratedRegex]` source generator (C# 12+) for the validation pattern `^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$`, which requires `partial` on the record. Pattern enforces: lowercase only, exactly two dot-separated segments (`action.resource`), each segment starts with a letter, no leading/trailing/consecutive dots, no spaces or special chars. Single-segment keys (e.g. `dashboard`) and 3+ segment keys (e.g. `admin.users.create`) are rejected per spec.
- **AuditableEntity**: Uses `DateTimeOffset?` for `DeletedAt`/`DeletedBy` (nullable) rather than non-nullable `DateTimeOffset` as the spec text implies. Nullable semantics are cleaner for "not deleted" state. `IsDeleted` property derived from `DeletedAt.HasValue`.
- **DeletionPolicy defaults**: Added defaults for all entity types per spec (User, RefreshToken, Role, MenuItem, Permission). RefreshToken default has all dimensions `false` since lifecycle is governed by revocation/expiration.

---

## Verification Results
- **Build**: ✅ 0 errors, 0 warnings
- **UnitTests**: ✅ 122/122 passed (53 Slice 1 + 69 Slice 2)
- **IntegrationTests**: ✅ 2/2 passed (pre-existing, unaffected)
- **ApplicationTests**: No tests (empty project, expected)
- **Domain.csproj**: ✅ Zero package references (BCL-only)
- **DateTimeOffset usage**: ✅ Confirmed — no `DateTime` types anywhere in Domain

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

---

## Remaining Tasks (Slice 3-5)
- [ ] 3.1-3.4 Core RBAC Entities (Permission, Role, RolePermission)
- [ ] 4.1-4.4 User & UserRole + superadmin guards
- [ ] 5.1-5.3 RefreshToken & MenuItem
- [ ] 6.1-6.3 Final Pass
