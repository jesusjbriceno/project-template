# Proposal: Domain Model

## Intent

Establish the first backend Domain model for identity, RBAC, refresh-token security, auditing, and deletion semantics. The Domain layer is currently empty; this change creates the behavioral foundation required before Application, Infrastructure, API, migrations, and UI authorization can be built safely.

## Scope

### In Scope
- Value objects: email and permission key.
- Entities/concepts: user, role, permission, user-role, role-permission, refresh token, menu item, auditable fields, and per-entity deletion policy.
- RBAC rules for `superadmin` and `user` system roles, administrable roles, permission copying, frontend menu authorization metadata, and refresh-token rotation/reuse detection.

### Out of Scope
- EF Core mappings, migrations, seeding, repositories, CQRS handlers, API endpoints, JWT generation, UI screens, employee concepts, role inheritance, and separate audit-log/event tracing.

## Capabilities

### New Capabilities
- `domain-model`: Core Domain entities, value objects, invariants, and unit-testable behavior for identity, RBAC, token security, auditing, and deletion policy.

### Modified Capabilities
- None.

## Proposed Domain Model and Invariants

- Users have normalized email, active state, opaque password hash, security stamp, role assignments, audit fields, and deletion fields.
- There MUST always be at least one active Superadmin user. The only active Superadmin cannot be deactivated, soft-deleted, or removed from effective access.
- Only a Superadmin can create another Superadmin, regardless of generic permissions.
- Permissions are functionality-defined catalog entries, not UI-administered. Roles are UI-administrable groupings of permissions; copying role permissions is point-in-time, not inheritance.
- Menu definitions MAY declare required permissions/roles for visibility; backend authorization remains authoritative.
- Refresh tokens store hashes only, rotate on use, detect reuse strictly, and revoke the token family/session on reuse.

## Deletion Policy Semantics

Soft delete, recycle-bin visibility, restore, and hard delete are independent policy dimensions, not synonyms. Each entity/table MUST define:

| Dimension | Meaning |
|---|---|
| `softDeleteEnabled` | Logical deletion/audit marker exists. |
| `recycleBinVisible` | Deleted records are user-visible in a recycle bin. |
| `restoreAllowed` | Deleted records can be restored. |
| `hardDeleteAllowed` | Physical removal is allowed. |

Examples: User enables soft delete for audit/security, disables recycle-bin restore, disables hard delete by default, and soft-deleted users are blocked from auth/authorization. RefreshToken is not a recycle-bin entity; model revocation, expiration, reuse detection, and later technical cleanup. Role may be soft-deleted/restorable only when consistency rules allow it; system roles are protected. MenuItem may be restorable/hard-deletable only when hierarchy consistency is preserved. This policy MUST flow into future implementer documentation.

## Approach

Use value-objects-first TDD: email → permission key → permissions/roles/junctions → users → refresh tokens → menu items → audit/deletion policy. Domain expresses guards; cross-user invariants are enforced transactionally by Application later. Follow `AGENTS.md` and `openspec/config.yaml` constraints.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `apps/api/src/Project.Domain/` | New | Domain objects and invariants |
| `apps/api/tests/Project.UnitTests/` | New | Failing tests first, then domain behavior |
| `openspec/specs/` | New | Future `domain-model` spec |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Last-Superadmin rule spans users | Med | Domain guard plus Application transaction later |
| Delete semantics conflated | Med | Model explicit per-entity policy dimensions |
| Token reuse handling is security-sensitive | Med | Specify family/session revocation and test edge cases |

## Rollback Plan

Revert Domain/UnitTests additions and remove this change's delta specs before any migration consumes the model. No persisted data changes are introduced.

## Open Questions / Deferred Decisions

- Exact folder structure, exception hierarchy, and strongly typed IDs are design-phase decisions.
- Separate audit-log/event tracing will be evaluated later.

## Success Criteria

- [ ] Domain behavior remains persistence/API independent.
- [ ] RBAC, Superadmin protection, token reuse, audit fields, and deletion policy are unambiguous.
- [ ] Future implementer docs carry the deletion policy dimensions forward.
