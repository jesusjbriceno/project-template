# Proposal: MigrationService Initial Seed

## Intent

Make deployment valid by turning the scaffolded MigrationService into the required database bootstrap path: apply EF migrations, seed current RBAC access data, create the initial Superadmin, then exit so Docker Compose can safely start the API.

Business value: a deployed environment must have a schema, baseline authorization catalog, and guaranteed administrative access without manual database edits.

## Scope

### In Scope
- Apply pending EF Core migrations through the dedicated MigrationService.
- Seed only current-state roles: `Superadmin` and `User`.
- Seed only the permission catalog known/needed by the current system state.
- Create the initial Superadmin from `SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD`; missing values fail the migration/seed process hard.
- Ensure initial seed data does not silently re-run, repair, or mutate existing roles, permissions, or Superadmin state.
- Add integration coverage for migration execution, seed creation, strict seed rerun behavior, and failure on missing Superadmin credentials.

### Out of Scope
- Future permissions, roles, menu seed data, API startup auto-migration, admin UI, password reset, or user management flows.
- Repairing or reconciling existing production RBAC data.

## Capabilities

### New Capabilities
- `migration-service`: Dedicated schema migration and initial seed runner for deployment bootstrap.

### Modified Capabilities
- None.

## Approach

Use the recommended BackgroundService approach: `MigrationWorker` creates a scoped `ApplicationDbContext`, runs `MigrateAsync()` through EF execution strategy, invokes a dedicated seed component, then stops the host. Seed creation uses existing Domain factories and Infrastructure persistence without changing Domain/Application contracts.

Schema migrations remain EF-history based and re-runnable. Initial seed data is stricter: after the first successful seed, follow-up catalog changes must be explicit controlled migrations/seeds, not implicit repair logic.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `apps/api/src/Project.MigrationService/` | Modified/New | Host wiring, worker, seed component, password hashing dependency. |
| `apps/api/tests/Project.IntegrationTests/MigrationService/` | New | PostgreSQL integration tests for migration and seed behavior. |
| `.env.example`, `docker-compose.yml` | Verify | Existing Superadmin env vars and migration gating must remain aligned. |

## Risks and Open Decisions

| Item | Risk | Resolution Path |
|------|------|-----------------|
| Password hashing library | Medium | Decide in design; must be secure and testable. |
| Exact permission catalog | Medium | Enumerate in spec from current implemented capabilities only. |
| Strict rerun semantics | Medium | Specify how seeded-state detection works and what failure message/logging proves. |

## Rollback and Deployment

Rollback by reverting the MigrationService change before redeploy. If a failed seed partially committed data, restore the database from backup or run an explicit corrective data migration; the service must not auto-repair silently. Failed credentials/configuration must exit non-zero and block API startup.

## Delivery Notes

Force chained PRs apply. Keep slices under the 400-line review budget: tests/spec foundation first, worker orchestration second, seed catalog/credential behavior third if needed.

## Success Criteria

- [ ] Migration container applies pending migrations and exits successfully.
- [ ] Superadmin/User roles, current permissions, and initial Superadmin are present after first run.
- [ ] Missing Superadmin credentials fail hard.
- [ ] Re-running seed does not mutate existing RBAC/Superadmin state silently.
