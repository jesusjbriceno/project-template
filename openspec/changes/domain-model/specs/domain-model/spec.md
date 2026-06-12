# domain-model Specification

## Purpose

Core Domain entities, value objects, invariants, and testable behavior for identity, RBAC, token security, auditing, and deletion policy. Zero persistence, ASP.NET, EF Core, or UI dependencies.

## Requirements

### Requirement: Value Objects (Email, PermissionKey)

Email MUST validate RFC 5322 pragmatically, normalize to lowercase, and use value-based equality. PermissionKey MUST use exactly two lowercase dot-separated segments (`action.resource`). Both SHALL reject null/empty/whitespace.

#### Scenario: Email normalization and equality

- GIVEN `"User@Example.com"` → Email normalizes to `"user@example.com"`
- THEN two Emails with same normalized value are equal

#### Scenario: PermissionKey validation

- GIVEN `"users.create"` (valid) and `"Users.Create"` (invalid uppercase)
- THEN valid succeeds; invalid fails

### Requirement: User Entity and Lifecycle

User SHALL hold normalized Email, IsActive flag, opaque password hash, security stamp, role assignments via UserRole, and DateTimeOffset audit fields. Deactivation SHALL set IsActive=false. Soft-deleted users (IsActive=false) MUST be functionally blocked from auth/authorization.

#### Scenario: Creation and deactivation

- GIVEN valid Email → User created: IsActive=true, CreatedAt set, security stamp generated
- GIVEN active User → deactivated: IsActive=false, UpdatedAt set

#### Scenario: Soft-deleted user blocked

- GIVEN User with IsActive=false
- THEN domain signals user is blocked from auth/authorization

### Requirement: Superadmin Protection

At least one active Superadmin MUST retain effective access. Only Superadmins SHALL create other Superadmins. The last active Superadmin MUST NOT be deactivated, soft-deleted, or removed from effective access.

#### Scenario: Last superadmin guard

- GIVEN exactly one active Superadmin
- WHEN deactivation or role removal attempted → domain invariant violation signaled

#### Scenario: Superadmin creation gate

- GIVEN User without Superadmin role
- WHEN creating User with Superadmin assignment → domain invariant violation signaled

### Requirement: RBAC Entities (Permission, Role, RolePermission, UserRole)

Permissions are functionality-defined catalog entries (not UI-administered), identified by PermissionKey. Roles are UI-administrable permission groupings linked via RolePermission junction. UserRole junction links User to Role with assignment audit (AssignedAt, AssignedBy). System roles (IsSystem=true) MUST NOT be deletable. Role permission copying is point-in-time only; no inheritance coupling.

#### Scenario: System role protection

- GIVEN Role with IsSystem=true → deletion attempt signals domain violation

#### Scenario: Permission copy is point-in-time

- GIVEN source Role with permissions [A,B] → target copies → receives A,B
- THEN subsequent source changes do NOT propagate to target

#### Scenario: User role assignment

- GIVEN User and Role → UserRole created → AssignedAt and AssignedBy recorded

### Requirement: RefreshToken Security

Refresh tokens MUST store only SHA-256 hashes (never raw). Tokens SHALL rotate on use: old token revoked with ReplacedByTokenHash. Reuse of a revoked token MUST trigger family/session revocation. Tokens SHALL carry ExpiresAt, with IsExpired(), IsRevoked(), IsActive() reflecting current state.

#### Scenario: Rotation revokes predecessor

- GIVEN valid token T1 → rotated producing T2 hash
- THEN T1.IsRevoked()=true, T1.ReplacedByTokenHash equals T2 hash

#### Scenario: Reuse detection revokes family

- GIVEN already-revoked token T1 → presented again
- THEN domain signals entire token family/session must be revoked

### Requirement: MenuItem Hierarchy

MenuItem SHALL support parent-child hierarchy via ParentId with SortOrder. Visibility MAY declare required PermissionKey/Role. Circular references MUST be prevented. Backend authorization remains authoritative.

#### Scenario: Hierarchy and cycle prevention

- GIVEN A→B→C chain → C.ParentId=B valid; C.ParentId=A triggers cycle violation

### Requirement: Deletion Policy Dimensions

Deletion dimensions SHALL be independent per entity (not synonyms): `softDeleteEnabled`, `recycleBinVisible`, `restoreAllowed`, `hardDeleteAllowed`. User defaults: soft-delete only (no recycle/restore/hard-delete). RefreshToken: no recycle bin; governed by revocation/expiration. Role/MenuItem: subject to consistency rules.

#### Scenario: Per-entity policy

- User entity: softDeleteEnabled=true, recycleBinVisible=false, restoreAllowed=false, hardDeleteAllowed=false
- RefreshToken entity: no recycle-bin semantics; lifecycle via revocation/expiration

### Requirement: Audit Fields

Full lifecycle entities SHALL carry DateTimeOffset audit fields: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy. Association/junction entities (RolePermission, UserRole) carry assignment-specific audit fields (AssignedAt, AssignedBy) instead, since they do not have independent create/update/delete lifecycle — they exist only while the association is active. DateTimeOffset MUST be used (never DateTime).

#### Scenario: Audit tracking

- New lifecycle entity: CreatedAt and UpdatedAt set to current DateTimeOffset
- Modified lifecycle entity: UpdatedAt and UpdatedBy reflect the change
- Association/junction entity (RolePermission, UserRole): AssignedAt and AssignedBy recorded at assignment time
