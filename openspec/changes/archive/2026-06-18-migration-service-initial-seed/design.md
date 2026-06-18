# Design: MigrationService Initial Seed

## Technical Approach

`MigrationWorker` (BackgroundService) opens a scoped `ApplicationDbContext`, validates credentials, runs `Database.MigrateAsync()` inside the Npgsql retry strategy, then invokes `SeedData.SeedAsync()` in the same scope. One `SaveChangesAsync` commits the seed as a single PostgreSQL transaction. On success the worker calls `IHostApplicationLifetime.StopApplication()` → exit 0 → Docker Compose unblocks the API. Seed uses only Domain factories and Application repository contracts. Idempotency via pre-insert existence checks; the Superadmin role is created first and passed as `actorRoles` in `User.AssignRole` to satisfy the Domain guard. All audit fields use `createdBy = "SYSTEM"`.

## Architecture Decisions

### Decision: Password hashing algorithm

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **BCrypt (`BCrypt.Net-Next` 4.x, cost 12)** | Single knob, ~3M daily downloads, OWASP-acceptable. | **Chosen** |
| Argon2id (`Isopoh.Cryptography.Argon2`) | OWASP 2024 first choice; memory-hard, but memory/iterations/parallelism tuning is environment-dependent and easy to misconfigure. | Rejected |
| PBKDF2 (`KeyDerivation`) | ASP.NET Identity-coupled; CPU-only, weaker than BCrypt at the same wall-clock. | Rejected |

Contract `IPasswordHasher { Hash, Verify }` in `Project.Application.Abstractions.Security`; impl in `Project.Infrastructure.Security` (singleton in `AddInfrastructure`). Swappable without Domain/Application impact.

### Decision: Role existence-by-name lookup

`IRoleRepository` lacks `GetByNameAsync`. Add `Task<Role?> GetByNameAsync(string, CancellationToken)` (minimal additive contract; `Name` is a business identifier). Impl: `Set.FirstOrDefaultAsync(r => r.Name == name, ct)`; soft-delete filter applies. Rejected: `GetSystemRolesAsync` + in-memory filter (leaks is-system-only assumption, inefficient).


### Decision: Permission catalog (current-state only)

22 keys, one-to-one with current Domain operations. Superadmin gets all 22; User gets none (no Application handlers exist; follow-up data migrations extend User).

- **users** (7): `read`, `create`, `update`, `deactivate`, `delete`, `assignRole`, `removeRole`
- **roles** (6): `read`, `create`, `update`, `delete`, `assignPermission`, `removePermission`
- **permissions** (1): `read`
- **menu** (5): `read`, `create`, `update`, `delete`, `reorder`
- **auth** (3): `refresh`, `revokeToken`, `revokeFamily`

### Decision: Transaction boundary

`SeedData` shares the `ApplicationDbContext` scope with `MigrateAsync`. All `AddAsync` calls accumulate; one `SaveChangesAsync` commits inside a single implicit PostgreSQL transaction. Any exception (Domain invariant, FK violation, DB error) rolls back the whole seed. Npgsql retry + EF implicit transaction suffice; no explicit `BeginTransactionAsync`.

### Decision: Failure behavior

Missing/whitespace credentials → log error naming variable (no value), throw, exit non-zero. `MigrateAsync` failure → log + re-throw, exit non-zero. Seed exception (e.g., `LastSuperadminGuardException`) → log + re-throw, exit non-zero. Domain `Create` invalid → propagates → exit non-zero. Second run → no-op. Password length ≥ 12 validated as defense in depth. Credentials are never logged.

## File Changes

| File | Action |
|------|--------|
| `apps/api/src/Project.MigrationService/Program.cs` | Modify: `AddInfrastructure` + worker + `host.Run()`. |
| `apps/api/src/Project.MigrationService/MigrationWorker.cs` | Create: `BackgroundService` orchestrator. |
| `apps/api/src/Project.MigrationService/SuperadminCredentialValidator.cs` | Create: `Result<SuperadminCredentials>`. |
| `apps/api/src/Project.MigrationService/SuperadminCredentials.cs` | Create: `record(Email, PlaintextPassword)`. |
| `apps/api/src/Project.MigrationService/PermissionCatalog.cs` | Create: 22 static entries. |
| `apps/api/src/Project.MigrationService/SeedData.cs` | Create: static `SeedAsync` (factories + one `SaveChangesAsync`). |
| `apps/api/src/Project.MigrationService/Project.MigrationService.csproj` | Modify: add `BCrypt.Net-Next 4.0.3`. |
| `apps/api/src/Project.Application/Abstractions/Security/IPasswordHasher.cs` | Create. |
| `apps/api/src/Project.Application/Abstractions/Persistence/IRoleRepository.cs` | Modify: add `GetByNameAsync`. |
| `apps/api/src/Project.Infrastructure/Security/BCryptPasswordHasher.cs` | Create: cost 12. |
| `apps/api/src/Project.Infrastructure/Data/Repositories/RoleRepository.cs` | Modify: implement `GetByNameAsync`. |
| `apps/api/src/Project.Infrastructure/DependencyInjection.cs` | Modify: register `IPasswordHasher`. |
| `apps/api/tests/Project.IntegrationTests/MigrationService/MigrationWorkerTests.cs` | Create: happy path, idempotency, missing-creds. |
| `apps/api/tests/Project.IntegrationTests/MigrationService/SeedDataTests.cs` | Create: catalog + re-run. |
| `apps/api/tests/Project.UnitTests/MigrationService/SuperadminCredentialValidatorTests.cs` | Create: `[Theory]`. |
| `apps/api/tests/Project.UnitTests/Security/BCryptPasswordHasherTests.cs` | Create: round-trip + verify. |



## Testing Strategy

| Layer | What | Approach |
|-------|------|----------|
| Unit | `SuperadminCredentialValidator` | `[Theory]` × 5: present, missing email, missing password, whitespace, invalid email. |
| Unit | `BCryptPasswordHasher` | Hash → verify-true; hash → verify wrong plaintext → false. |
| Integration | Worker happy path | `PostgresFixture` conn string; run worker; assert `__EFMigrationsHistory` populated, Superadmin user+role exist. |
| Integration | Idempotency | Run worker twice; row counts and `UpdatedAt` unchanged. |
| Integration | Missing credentials | `SUPERADMIN_PASSWORD=""` → exception with variable name. |
| Integration | Catalog completeness | All 22 `PermissionKey` rows; `RolePermission` for Superadmin = 22; `UserRole` for Superadmin user = {Superadmin role}. |
| Integration | Strict re-run | Snapshot `User.PasswordHash`/`SecurityStamp`, `Role.IsSystem`, `Permission.Description`; re-run; assert byte-equal. |



## Migration / Rollout

No new EF Core migration files; `20260616155722_InitialInfrastructureSchema` already exists. Rollback = revert this change. Failed seed recovery = restore DB from backup or run corrective data migration; no auto-repair. The seed is idempotent — a new key added later is inserted on the next run; removing/renaming a key requires deliberate data migration.

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Partial seed commit on crash | Single `SaveChangesAsync` inside one implicit transaction. |
| Future `User.AssignRole` change breaks bootstrap | Synthetic `actorRoles` invariant in `SeedData`; test asserts role assignment. |
| Weak `SUPERADMIN_PASSWORD` in `.env` | Validator enforces length ≥ 12 (defense in depth). |
| Fixture `EnsureCreatedAsync` schema diverges from `MigrateAsync` | Tests call `MigrateAsync` directly. |
| Catalog drift | Tests enumerate expected keys; editing catalog without updating tests fails CI. |

## Open Questions

None. Both spec open decisions are resolved.
