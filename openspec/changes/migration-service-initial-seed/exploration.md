# Exploration: MigrationService + Initial Seed

## Current State

The MigrationService project exists as a scaffold (`Program.cs` exits immediately with a log message). Every other dependency is already wired: Docker Compose orchestrates `db → migration → api` with `service_completed_successfully` gates; the migration Dockerfile copies Infrastructure and publishes the MigrationService project; environment variables `SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD` are already declared in `.env.example` and passed to the migration container.

The Infrastructure layer (EF Core) is fully built and archived:
- `ApplicationDbContext` with NoTracking, SplitQuery, NpgsqlRetryingExecutionStrategy, and audit interceptor.
- 7 entity configurations (User, Role, Permission, RefreshToken, MenuItem, UserRole, RolePermission) fully mapped to PostgreSQL 17.
- 7 strongly-typed ID converters/comparers, 3 value-object converters (Email, PermissionKey, DeletionPolicy).
- 5 per-aggregate repositories (`UserRepository`, `RoleRepository`, `PermissionRepository`, `RefreshTokenRepository`, `MenuItemRepository`) implementing Application-layer contracts.
- `AddInfrastructure(IServiceCollection, string connectionString)` extension binds `IClock→SystemClock`, `IUserSession→NullUserSession`, and all repositories.
- Initial migration `20260616155722_InitialInfrastructureSchema` exists (generated, not auto-applied).

The Domain layer provides factory methods for all seedable entities:
- `Permission.Create(PermissionKey key, string description, string? category, string createdBy, IClock clock)`
- `Role.Create(string name, bool isSystem, string createdBy, IClock clock)`
- `User.Create(Email email, string passwordHash, string createdBy, IClock clock)`
- `Role.AddPermission(Permission permission, string assignedBy, IClock clock)`
- `User.AssignRole(Role role, string assignedBy, IClock clock, IReadOnlyCollection<Role>? actorRoles)`

**Critical bootstrap gate in `User.AssignRole`**: The method enforces that only superadmins can assign the superadmin role — `actorRoles` MUST contain a superadmin role. The Domain explicitly documents: *"Bootstrap/seed scenarios must provide the superadmin role explicitly through a dedicated seeding operation outside this normal method."* The seed can satisfy this by passing the superadmin role itself in `actorRoles` — a synthetic "SYSTEM" bootstrap context.

## Affected Areas

| File / Area | Why Affected |
|---|---|
| `apps/api/src/Project.MigrationService/Program.cs` | Rewrite: wire `AddInfrastructure`, host builder, register MigrationWorker |
| `apps/api/src/Project.MigrationService/MigrationWorker.cs` | **New**: BackgroundService that applies pending migrations, invokes seed, then stops the host |
| `apps/api/src/Project.MigrationService/SeedData.cs` | **New**: Static seeder — idempotent seed of permissions, system roles, superadmin user |
| `apps/api/src/Project.MigrationService/Project.MigrationService.csproj` | Add password hashing NuGet (BCrypt or Argon2) |
| `apps/api/tests/Project.IntegrationTests/MigrationService/` | **New**: Integration tests for migration application + seed verification + idempotency |
| `docker-compose.yml` | Already wired — no changes needed unless connection string format shifts |
| `infra/docker/migration.Dockerfile` | Already copies Infrastructure project; no changes expected |
| `.env.example` | Already declares `SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD` |

**Not affected**:
- `Project.Api.Controllers/Program.cs` — the API startup convenience for local dev (auto-migrate on `Development`) is a separate concern, explicitly deferred in ROADMAP.md and infrastructure-ef-core design doc.
- Domain and Application layers — zero changes. Seed uses existing Domain factories and Application repository contracts.
- Existing integration tests — they use `EnsureDeletedAsync`/`EnsureCreatedAsync` and are independent of migration application.

## Approaches

### Approach A: BackgroundService + Dedicated SeedData class (Recommended)

**Pattern**: `MigrationWorker : BackgroundService` resolves `ApplicationDbContext` in a scoped service. In `ExecuteAsync`: check pending migrations → `MigrateAsync()` → invoke `SeedData.SeedAsync()` (idempotent) → `StopApplication()`. `SeedData` is a static helper class that receives `ApplicationDbContext`, `IClock`, and raw `SUPERADMIN_*` config values.

- **Pros**:
  - Follows the EF Core Patterns skill pattern (dedicated migration service, execution strategy, stop application).
  - Matches the code comments already in the scaffold (`// Future: builder.Services.AddHostedService<MigrationWorker>()`).
  - `SeedData` is testable in isolation (pass mocks/stubs for DbContext + IClock).
  - Clear separation: `MigrationWorker` orchestrates; `SeedData` contains the seed logic.
  - Container lifecycle: `host.Run()` blocks until `StopApplication()` → Docker `service_completed_successfully` works correctly.

- **Cons**:
  - `BackgroundService` adds minor boilerplate (class, DI constructor, scope creation).
  - SeedData must receive the connection string / config values via some injection path.

- **Effort**: Medium (~250–350 lines total)

---

### Approach B: Direct Migration + Seed in Program.cs (No BackgroundService)

**Pattern**: After `builder.Build()`, directly resolve `ApplicationDbContext` via a scope, call `MigrateAsync()` and seed synchronously, then return. No `host.Run()` — just a console app that exits.

- **Pros**:
  - Minimal boilerplate. No `BackgroundService` class, no `IHostApplicationLifetime`.
  - Simpler control flow — synchronous-style linear code.

- **Cons**:
  - Bypasses the established EF Core Patterns skill recommendation (dedicated migration worker).
  - Harder to test: the migration + seed logic is embedded in `Program.cs` (top-level statements), which xUnit cannot directly test against a fixture.
  - No execution strategy retry wrapper (must hand-roll or skip).
  - Doesn't match the scaffold's designed intent (comments explicitly describe `MigrationWorker`).

- **Effort**: Low (~100–150 lines) but lower testability and architectural fit.

---

### Approach C: API Startup Convenience (Separate from MigrationService)

**Pattern**: Allow `Project.Api.Controllers` to optionally run `MigrateAsync()` on startup when `ASPNETCORE_ENVIRONMENT=Development`. This is a separate feature from the MigrationService — the ROADMAP lists it as a convenience option.

- **Pros**: Convenient for local dev (no separate container needed).
- **Cons**: Mixes concerns; violates the principle that API should wait for migrations, not perform them. Already deferred in ROADMAP.
- **Effort**: N/A — not part of this change.

---

## Recommendation

**Approach A — BackgroundService + Dedicated SeedData class.**

This follows the established pattern from the EF Core Patterns skill, matches the design intent written in the scaffold code comments, and aligns with the force-chained delivery strategy used throughout the project. The `MigrationWorker` orchestrates three distinct phases (migrate, seed, stop) with clean separation and full testability via integration tests against PostgreSQL Testcontainers.

### Key Design Decisions Implied

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **Password hashing library** | Must be selected. BCrypt (`BCrypt.Net-Next`) is the project's stated direction (from infra-ef-core exploration). Argon2id is a modern alternative but BCrypt is simpler and widely adopted. Decision belongs to design phase. |
| 2 | **Superadmin bootstrap** | Pass the superadmin `Role` itself in `actorRoles` when calling `User.AssignRole()`. This satisfies the Domain guard (actorRoles contains a superadmin role) without code changes. |
| 3 | **Seed idempotency** | Check-by-key/email before insert: `IPermissionRepository.ExistsByKeyAsync`, `IRoleRepository` (by name via set query), `IUserRepository.GetByEmailAsync`. No EF Core `OnConflict` — explicit guards at the Application layer. |
| 4 | **Permission catalog scope** | Seed only the permissions needed for the initial system: ~30–40 permissions across `users.*`, `roles.*`, `permissions.*`, `menu.*`, and `auth.*` domains. The exact catalog should be defined in the spec/specs phase. |
| 5 | **Role catalog** | At minimum: `Superadmin` (IsSystem=true, all permissions). Additional roles (`Admin`, `User`) can be included or deferred. |
| 6 | **SeededBy identity** | Use `"SYSTEM"` as the `createdBy`/`assignedBy` string for all seed entities. This distinguishes seed-created entities from user-created ones. |
| 7 | **Connection string source** | Read from `ConnectionStrings__DefaultConnection` in configuration (already passed via Docker Compose env). |
| 8 | **Superadmin password** | Read `SUPERADMIN_PASSWORD` from `IConfiguration`; hash via chosen hashing library before passing to `User.Create`. |

### Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| **Integration** | Migration applies schema | Verify `GetPendingMigrationsAsync()` returns empty after migrate; verify tables exist. |
| **Integration** | Seed inserts permissions | Assert permission count > 0; verify key uniqueness. |
| **Integration** | Seed inserts system roles | Assert Superadmin role exists with `IsSystem=true`; verify role-permission assignments. |
| **Integration** | Seed inserts superadmin user | Assert user exists with expected email; verify Superadmin role assignment. |
| **Integration** | Idempotency | Run seed twice; assert counts unchanged (permissions, roles, user). |
| **Integration** | MigrationWorker lifecycle | Prove that running the MigrationWorker applies migrations, seeds data, and stops. |

All integration tests use the existing `PostgresFixture` (`PostgreSqlContainer` with `postgres:17-alpine`) already available in `Project.IntegrationTests`. The fixture's `InitializeAsync` currently calls `EnsureDeletedAsync`/`EnsureCreatedAsync` — tests for MigrationService must instead call `MigrateAsync()` from the DbContext to validate real migration application.

No unit tests for `MigrationWorker` (orchestration only — no pure logic). `SeedData` can have unit tests if extracted with injectable dependencies, but integration tests are the primary verification since seed correctness is database-dependent.

### TDD Flow

Per `strict_tdd: true`:
1. Write integration test: "After MigrationWorker runs, superadmin user exists with correct email" → **RED** (no worker, no seed)
2. Implement `MigrationWorker` + `SeedData` → **GREEN**
3. Write integration test: "Seed is idempotent" → **GREEN** (already designed idempotent) or add guards → **GREEN**
4. Write integration test: "Pending migrations are applied" → **GREEN**

## Risks

- **Password hashing library not yet selected**: Decision needed in design phase. BCrypt (`BCrypt.Net-Next`) is the stated direction but has not been added as a NuGet dependency yet. Argon2id is a worthy alternative. Either choice requires a NuGet reference in `Project.MigrationService.csproj`.

- **Permission catalog definition**: The exact set of seed permissions (keys, descriptions, categories) is not yet defined. This is a spec concern — the exploration must flag that the spec phase needs to enumerate the catalog.

- **Superadmin bootstrap guard**: The Domain's `User.AssignRole` guard requires `actorRoles` containing superadmin. The synthetic bootstrap context (passing the role itself) works, but it's a single point of failure if the guard logic changes in a future Domain refactor. The spec should call out this invariant explicitly.

- **Idempotency with concurrent seeds**: In a single-instance Docker deployment, only one migration container runs at a time — race conditions are not a concern. But the seed logic should still be written defensively (existence checks before inserts) as a general best practice.

- **Seed with EF Core NoTracking**: `ApplicationDbContext` uses `NoTracking` globally. Seed operations that create entities via Domain factories and call `AddAsync` (which goes through `DbSet.AddAsync`) are unaffected — tracking is only relevant for updates to already-tracked instances. The seed creates new entities and adds them, which works correctly with NoTracking.

- **Migration idempotency**: `MigrateAsync()` is already idempotent (EF Core tracks applied migrations in the `__EFMigrationsHistory` table). No additional work needed.

- **Docker Compose timing**: The `depends_on: migration: service_completed_successfully` on the API service relies on the migration container exiting with code 0. `hostApplicationLifetime.StopApplication()` triggers a graceful shutdown and should exit cleanly. Any unhandled exception in the MigrationWorker will crash the container with non-zero exit code, correctly preventing API startup.

## Ready for Proposal

**Yes.** The exploration has identified:
- The exact gap between the MigrationService scaffold and the target behavior (apply migrations → seed data → exit).
- All affected files (4 new/modified source files + new test directory).
- A clear architectural approach (BackgroundService + SeedData, following EF Core Patterns skill).
- The superadmin bootstrap mechanism (synthetic actorRoles context).
- The seed idempotency strategy (existence checks via repository contracts or direct set queries).
- A testing strategy aligned with existing integration test patterns (PostgresFixture + Testcontainers).
- Key open questions to resolve in spec/design: password hashing library choice, exact permission catalog, and whether additional system roles beyond Superadmin should be seeded.

The orchestrator should launch `sdd-propose` next to create a formal change proposal with Scope, Approach, Rollback Plan, and delivery strategy.

## Open Questions for Spec / Design

1. **Which password hashing algorithm?** BCrypt (`BCrypt.Net-Next`) or Argon2id (`Konscious.Security.Cryptography.Argon2`)?
2. **Exact permission catalog?** Which `action.resource` keys should be seeded? This defines the permission baseline for the entire system.
3. **Additional system roles?** Beyond Superadmin — should `Admin`, `User`, or other roles be seeded?
4. **API dev convenience?** The ROADMAP mentions allowing API startup migrations in Development mode. Is this a separate follow-up change, or partially in scope? (Recommendation: separate change.)
