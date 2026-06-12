# Tasks: Domain Model

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~1600-2000 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | 5 feature branches → `develop` |
| Delivery strategy | auto-chain |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

**Delivery model**: Git Flow feature branches. Each slice is an independent feature branch targeting `develop`. Validated slices merge into `develop`. A release is cut from `develop` to `main` once a coherent version is ready.

| Unit | Goal | Feature branch | Notes |
|------|------|----------------|-------|
| 1 | ID primitives + domain errors | `feature/domain-model-01-ids-errors` | Base: `develop`; ~300 lines |
| 2 | Value objects + policies + clock | `feature/domain-model-02-value-objects` | Base: `develop`; ~350 lines |
| 3 | Permission + Role + RolePermission | `feature/domain-model-03-rbac-core` | Base: `develop`; ~400 lines |
| 4 | User + UserRole + superadmin guards | `feature/domain-model-04-users` | Base: `develop`; ~400 lines |
| 5 | RefreshToken + MenuItem + final pass | `feature/domain-model-05-tokens-menu` | Base: `develop`; ~350 lines |

## Phase 1: Foundation (IDs + Errors)

- [x] 1.1 Create `ValueObjects/Ids/*.cs` — 7 ID types: 5 simple (`UserId`, `RoleId`, `PermissionId`, `RefreshTokenId`, `MenuItemId`) as `sealed class` + `IEquatable<T>` with private constructors, `New()`/`From(Guid)`, implicit `Guid` conversion; 2 composite (`UserRoleId`, `RolePermissionId`) with `From()` factory, null guards, `Deconstruct()`
- [x] 1.2 Write ID tests (34 tests): `New()`/`From()`/`From(Guid.Empty)` guard/equality/implicit conversion/`default` null/composite null-guards/Deconstruct
- [x] 1.3 Create `Errors/DomainException.cs` base class + 7 sealed derivatives: `LastSuperadminGuardException`, `SystemRoleProtectedException`, `MenuCycleDetectedException`, `RefreshTokenReuseSignalException`, `InvalidEmailException`, `InvalidPermissionKeyException`, `DeletionPolicyViolationException`
- [x] 1.4 Write domain exception tests: instantiation, message, inheritance

## Phase 2: Value Objects & Policies

- [x] 2.1 (RED→GREEN) `ValueObjects/Email.cs` — pragmatic RFC 5322 validation, full lowercase normalization, value equality; tests for valid/invalid/null formats, equality
- [x] 2.2 (RED→GREEN) `ValueObjects/PermissionKey.cs` — lowercase `action.resource` validation; tests for valid keys, uppercase/spaces rejection, equality
- [x] 2.3 Create `Policies/DeletionPolicy.cs` — sealed record `(SoftDeleteEnabled, RecycleBinVisible, RestoreAllowed, HardDeleteAllowed)` + static defaults per entity
- [x] 2.4 Write DeletionPolicy tests: User defaults (soft-only), RefreshToken defaults, dimension access
- [x] 2.5 Create `Common/IClock.cs` + `SystemClock.cs` — UTC `UtcNow` abstraction
- [x] 2.6 Create `Common/AuditableEntity.cs` — `DateTimeOffset` CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/DeletedAt/DeletedBy + lifecycle methods
- [x] 2.7 Write AuditableEntity tests: fields set on creation, update modifies UpdatedAt
- [x] 2.8 Fix Email validation gaps (no local/domain part, spaces, no TLD dot) + PermissionKey segment enforcement (exactly 2 segments). Review fix: 8 critical findings resolved.

## Phase 3: Core RBAC Entities

- [x] 3.1 (RED→GREEN) `Entities/Permission.cs` — `PermissionKey`, `Description`, `Category`; tests for creation with valid key
- [x] 3.2 (RED→GREEN) `Entities/Role.cs` — `Name`, `IsSystem`, `CopyPermissionsTo(Role)` point-in-time; tests for system role deletion guard
- [x] 3.3 (RED→GREEN) `Entities/RolePermission.cs` — `RolePermissionId` composite, `AssignedAt`/`AssignedBy`, `Assign()` factory; tests for assignment audit
- [x] 3.4 Write integrated Role tests: AddPermission/RemovePermission, CopyPermissions point-in-time isolation

## Phase 4: User & UserRole

- [x] 4.1 (RED→GREEN) `Entities/User.cs` — `Email`, `IsActive`, `PasswordHash`, `SecurityStamp`, `LastLoginAt`, `UserRoles`; tests for creation, deactivation, soft-delete auth block
- [x] 4.2 (RED→GREEN) `Entities/UserRole.cs` — `UserRoleId` composite, `AssignedAt`/`AssignedBy`, `Assign()` factory; tests for assignment tracking
- [x] 4.3 Write `User.RemoveRole()` with `IReadOnlyCollection<User>` superadmin guard: 0/1/2 active Superadmins boundary
- [x] 4.4 Write `User.AssignRole()` superadmin gate: non-Superadmin assigning Superadmin signals domain violation

## Phase 5: RefreshToken + MenuItem

- [ ] 5.1 (RED→GREEN) `Entities/RefreshToken.cs` — `TokenHash`, `FamilyId`, `ExpiresAt`, `Rotate()`, `IsExpired()`, `IsRevoked()`, `IsActive()`, `IsReuseSignal()`; rotation + reuse detection tests
- [ ] 5.2 Write RefreshToken chain test: T1→T2→T3, assert T1.IsReuseSignal() after T2 rotates
- [ ] 5.3 (RED→GREEN) `Entities/MenuItem.cs` — `Label`, `Icon`, `Route`, `ParentId`, `SortOrder`, `RequiredPermissionKey`, `RequiredRoleId`, `IsVisible`, `SetParent()` cycle guard; hierarchy + cycle tests

## Phase 6: Final Pass

- [ ] 6.1 Run `dotnet test apps/api` — all domain unit tests green
- [ ] 6.2 Verify Domain project has zero EF Core/ASP.NET/UI package dependencies
- [ ] 6.3 Verify all spec scenarios have at least one covering test
