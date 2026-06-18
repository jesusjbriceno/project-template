# Tasks: MigrationService Initial Seed

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~620 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (foundation) → PR 2 (seed) → PR 3 (worker + integration) |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Base Branch | Verification |
|------|------|-----------|-------------|--------------|
| 1 | Password hasher + repo contract | PR 1 | feature/tracker | Unit tests pass |
| 2 | Seed components + credential validation | PR 2 | PR #1 branch | Unit + integration tests |
| 3 | MigrationWorker + wiring + end-to-end | PR 3 | PR #2 branch | Full integration suite |

## Phase 1: Foundation — Contracts + Password Hasher

- [x] 1.1 Create `IPasswordHasher` in `Project.Application.Abstractions.Security`
- [x] 1.2 Add `GetByNameAsync(string, CancellationToken)` to `IRoleRepository`
- [x] 1.3 Implement `BCryptPasswordHasher` (cost 12) in `Project.Infrastructure.Security`
- [x] 1.4 Implement `GetByNameAsync` in `RoleRepository` (FirstOrDefaultAsync by Name)
- [x] 1.5 Register `IPasswordHasher` singleton in `DependencyInjection.cs`
- [x] 1.6 [TDD-RED→GREEN] `BCryptPasswordHasherTests`: 10 tests covering hash, verify, wrong password, null guards, malformed hash, and salt uniqueness

## Phase 2: Core — Seed Components

- [x] 2.1 Create `SuperadminCredentials` record (Email, PlaintextPassword)
- [x] 2.2 Create `SuperadminCredentialValidator` (presence, length≥12, valid email)
- [x] 2.3 Create `PermissionCatalog` with 22 static entries (users×7, roles×6, permissions×1, menu×5, auth×3)
- [x] 2.4 Create `SeedData.SeedAsync`: idempotent role/permission/user creation via Domain factories, synthetic actorRoles for Superadmin role assignment
- [x] 2.5 [TDD-RED→GREEN] `SuperadminCredentialValidatorTests` [Theory]×8
- [x] 2.6 [TDD-RED→GREEN] `PermissionCatalogTests` [Fact]×1 (forces static init, validates 22 unique regex-safe keys)

## Phase 3: Integration — Worker + Wiring + End-to-End

- [x] 3.1 Create `MigrationWorker` BackgroundService: validate→MigrateAsync→SeedAsync→StopApplication
- [x] 3.2 Modify `Program.cs`: AddInfrastructure + MigrationWorker registration + host.Run
- [x] 3.3 Add `BCrypt.Net-Next 4.0.3` to `Project.MigrationService.csproj`
- [x] 3.4 [TDD-RED→GREEN] `MigrationWorkerTests`: happy path, missing-creds abort, re-run idempotency (PostgreSQL Testcontainers)
- [x] 3.5 [TDD-RED→GREEN] `SeedDataTests`: catalog completeness (22 permissions), strict re-run snapshot equality
