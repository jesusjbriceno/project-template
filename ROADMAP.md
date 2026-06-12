# Project Roadmap

This roadmap tracks the implementation state of the base project. It is updated after each completed slice so reviewers and implementers can see what is done, what is in progress, and what remains.

## Current Status

| Area | Status | Notes |
|------|--------|-------|
| Repository scaffold | ✅ Done | Initial monorepo, Docker, API health endpoint, OpenSpec, AGENTS.md, README.md. |
| Git Flow | ✅ Active | Work is organized in feature branches from `develop`; releases will be promoted to `main`. |
| Domain model planning | ✅ Done | Proposal, spec, design, tasks, and apply progress exist under `openspec/changes/domain-model/`. |
| Domain model implementation | 🟡 In progress | Slices 1–3 merged to `develop`; Slice 4 soft-delete superadmin guard implemented, pending fresh review. |

## Domain Model Slices

| Slice | Branch | Status | Summary | Verification |
|-------|--------|--------|---------|--------------|
| 1. IDs + domain errors | `feature/domain-model-01-ids-errors` | ✅ Merged to `develop` | Strongly typed ID value objects and domain exception hierarchy. | Unit/integration tests passed; fresh review passed. |
| 2. Value objects + policies | `feature/domain-model-02-value-objects` | ✅ Merged to `develop` | `Email`, `PermissionKey`, `DeletionPolicy`, `IClock`, `SystemClock`, `AuditableEntity`. | Validation gaps fixed; unit/integration tests passed; fresh review passed. |
| 3. RBAC core | `feature/domain-model-03-rbac-core` | ✅ Merged to `develop` | `Permission`, `Role`, `RolePermission`, role permission copy, system-role deletion guard. | Bypass fixed with `virtual`/`override`; unit/integration tests passed; fresh review passed. |
| 4. Users + Superadmin guards | `feature/domain-model-04-users` | 🟡 In progress — soft-delete guard fixed | `User`, `UserRole`, Superadmin assignment/removal/deactivation/delete guards. | Soft-delete guard implemented: `MarkDeleted` → `protected` + `User.Delete(activeSuperadmins)` with last-superadmin guard. Pending fresh review. |
| 5. Tokens + menu | `feature/domain-model-05-tokens-menu` | ⬜ Pending | `RefreshToken`, rotation/reuse detection, `MenuItem` hierarchy and cycle guards. | Not started. |
| 6. Final pass | TBD | ⬜ Pending | Final domain review, cleanup, docs alignment, full verification. | Not started. |

## Immediate Next Actions

1. ~~Fix Slice 4 blocker: `User.MarkDeleted()` must not soft-delete the only active Superadmin.~~ ✅ Done — `AuditableEntity.MarkDeleted` is now `protected`; `User.Delete(activeSuperadmins, ...)` enforces last-superadmin guard.
2. ~~Add regression tests for the soft-delete path.~~ ✅ Done — 3 new tests + API shape reflection test.
3. ~~Update `openspec/changes/domain-model/apply-progress.md` after the fix.~~ ✅ Done.
4. Run full build/tests and fresh review.
5. Commit Slice 4 only when review passes.
6. Merge Slice 4 into `develop`.
7. Create `feature/domain-model-05-tokens-menu`.

## Backlog by Phase

### Backend Domain

- ✅ Strongly typed IDs and exception hierarchy.
- ✅ Value objects and common domain foundations.
- ✅ RBAC core entities.
- 🔴 Finish User/Superadmin lifecycle invariants.
- ⬜ Implement refresh token domain model.
- ⬜ Implement menu item hierarchy domain model.

### Backend Application

- ⬜ Result pattern.
- ⬜ CQRS abstractions.
- ⬜ FluentValidation pipeline.
- ⬜ Repository interfaces.
- ⬜ Superadmin transactional enforcement in use cases.

### Backend Infrastructure

- ⬜ EF Core DbContext.
- ⬜ PostgreSQL mappings.
- ⬜ Typed ID conversions.
- ⬜ Migration creation.
- ⬜ Repository implementations.
- ⬜ Seed system roles, permissions, and initial Superadmin.

### Migration Strategy

- ✅ Keep dedicated `MigrationService` for controlled deployments.
- ✅ Allow API startup migrations/seeds only for Local/Development convenience, gated by configuration.
- ⬜ Implement real MigrationService once Infrastructure exists.

### API

- ⬜ Auth endpoints.
- ⬜ Users/Roles/Permissions controllers.
- ⬜ Permission authorization policies.
- ⬜ ProblemDetails/Result mapping.
- ⬜ OpenAPI metadata.

### Frontend

- ⬜ Vite + React + TypeScript scaffold.
- ⬜ Tailwind theme tokens and light/dark/system modes.
- ⬜ TanStack Router setup.
- ⬜ Zustand auth/session store.
- ⬜ Zod schemas.
- ⬜ Login/session UI.
- ⬜ Users/Roles/Permissions management UI.

### Quality Gates

- ⬜ `.editorconfig` and formatter workflow.
- ⬜ Sonar/static analysis baseline.
- ⬜ Dependency scanning.
- ⬜ Secret scanning.
- ⬜ OWASP checklist.
- ⬜ Accessibility checks for frontend views.

## Update Rule

After each slice:

1. Update the matching slice row status.
2. Add the commit hash and branch result if available.
3. Update Immediate Next Actions.
4. Keep OpenSpec artifacts as the source of detailed requirements and verification evidence.
5. Do not mark a slice ✅ until tests and fresh review pass.
