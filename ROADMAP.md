# Project Roadmap

This roadmap tracks the implementation state of the base project. It is updated after each completed slice so reviewers and implementers can see what is done, what is in progress, and what remains.

## Current Status

| Area | Status | Notes |
|------|--------|-------|
| Repository scaffold | ✅ Done | Initial monorepo, Docker, API health endpoint, OpenSpec, AGENTS.md, README.md. |
| Git Flow | ✅ Active | Work is organized in feature branches from `develop`; releases will be promoted to `main`. |
| Domain model planning | ✅ Archived | Source-of-truth spec exists at `openspec/specs/domain-model/spec.md`; audit trail archived under `openspec/changes/archive/2026-06-14-domain-model/`. |
| Domain model implementation | ✅ Done | All 6 slices complete and archived. 26/26 tasks done. 241 tests green. BCL-only verified. |
| Application layer | ✅ Archived | Source-of-truth spec exists at `openspec/specs/application-layer/spec.md`; audit trail archived under `openspec/changes/archive/2026-06-15-application-layer/`. |
| Infrastructure EF Core | ✅ Archived | Source-of-truth spec exists at `openspec/specs/infrastructure-ef-core/spec.md`; audit trail archived under `openspec/changes/archive/2026-06-17-infrastructure-ef-core/`. |
| MigrationService + initial seed | 🔵 Selected next | Next feature: apply EF Core migrations at startup and seed system roles, permissions, and initial Superadmin. |

## Domain Model Slices

| Slice | Branch | Status | Summary | Verification |
|-------|--------|--------|---------|--------------|
| 1. IDs + domain errors | `feature/domain-model-01-ids-errors` | ✅ Merged to `develop` | Strongly typed ID value objects and domain exception hierarchy. | Unit/integration tests passed; fresh review passed. |
| 2. Value objects + policies | `feature/domain-model-02-value-objects` | ✅ Merged to `develop` | `Email`, `PermissionKey`, `DeletionPolicy`, `IClock`, `SystemClock`, `AuditableEntity`. | Validation gaps fixed; unit/integration tests passed; fresh review passed. |
| 3. RBAC core | `feature/domain-model-03-rbac-core` | ✅ Merged to `develop` | `Permission`, `Role`, `RolePermission`, role permission copy, system-role deletion guard. | Bypass fixed with `virtual`/`override`; unit/integration tests passed; fresh review passed. |
| 4. Users + Superadmin guards | `feature/domain-model-04-users` | ✅ Merged to `develop` | `User`, `UserRole`, Superadmin assignment/removal/deactivation/delete guards. |
| 5. Tokens + menu | `feature/domain-model-05-tokens-menu` | ✅ Done | `RefreshToken`, `MenuItem`. Rotation/reuse detection/cycle guards implemented. 47 targeted tests. |
| 6. Final pass | `feature/domain-model-06-final-pass` | ✅ Done | Full verification: 241 tests green, BCL-only confirmed, 14/14 spec scenarios covered. | Unit 239 + Integration 2 = 241 passed. |

## Immediate Next Actions

1. ✅ Slice 4 soft-delete superadmin guard fixed and merged to `develop`.
2. ✅ Slice 5 RefreshToken + MenuItem implemented on `feature/domain-model-05-tokens-menu`.
3. ✅ Slice 5 fresh review fixes applied (5 findings: SHA-256 enforcement, expired rotate guard, reuse detection, mutation order, Guid.Empty familyId).
4. ✅ Slice 6 final pass completed: 241 tests green, BCL-only verified, 14/14 spec scenarios covered.
5. ✅ Domain-model verified, archived, and main spec synced.
6. ✅ Application layer verified, archived, and main spec synced.
7. ✅ Infrastructure EF Core verified, archived, merged to `develop`, and main spec synced.
8. 🔵 Selected next feature: MigrationService + initial seed — apply migrations and seed system roles, permissions, and initial Superadmin.

## Backlog by Phase

### Backend Domain

- ✅ Strongly typed IDs and exception hierarchy.
- ✅ Value objects and common domain foundations.
- ✅ RBAC core entities.
- ✅ Finish User/Superadmin lifecycle invariants.
- ✅ Implement refresh token domain model.
- ✅ Implement menu item hierarchy domain model.

### Backend Application

- ✅ Result pattern.
- ✅ CQRS abstractions.
- 🟡 FluentValidation pipeline marker exists; concrete pipeline behavior is deferred to use-case slices.
- ✅ Repository interfaces.
- ✅ Generic auth/session/security contracts.
- ⬜ Superadmin transactional enforcement in use cases.

### Backend Infrastructure

- ✅ EF Core DbContext.
- ✅ PostgreSQL mappings.
- ✅ Typed ID conversions.
- ✅ Migration creation.
- ✅ Repository implementations.
- ⬜ Seed system roles, permissions, and initial Superadmin.

### Migration Strategy

- ✅ Keep dedicated `MigrationService` for controlled deployments.
- ✅ Allow API startup migrations/seeds only for Local/Development convenience, gated by configuration.
- 🔵 Implement real MigrationService + initial seed as the selected next feature.

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
- ✅ Sonar/static analysis baseline — CI workflow lives at `.github/workflows/sonar-develop.yml` against `project-template` project key; triggers on push to `develop` only (SonarQube Community branch limitation). The SonarScanner for .NET v8+ ignores `sonar-project.properties`; all properties are passed via `dotnet sonarscanner begin` with `sonar.projectBaseDir=apps/api`. Coverage is imported from `coverlet.collector` Cobertura output into `apps/api/coverage/coverage.cobertura.xml` and read by the .NET scanner via `sonar.cs.cobertura.reportsPaths`. Workflow run `27541345420` passed on `develop` after adding a SonarQube availability check, scanner timeout settings, and stopping `sonarscanner end` from running after a failed `begin`.
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
