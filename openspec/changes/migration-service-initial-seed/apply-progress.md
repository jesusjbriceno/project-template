# Apply Progress: MigrationService Initial Seed

## Batch: Phase 1 / PR 1 — Foundation

**Date**: 2026-06-17
**Mode**: Standard (project TDD: write failing tests first where practical)
**Work unit**: Foundation contracts + password hasher + role lookup

### Completed Tasks

- [x] 1.1 Create `IPasswordHasher` in `Project.Application.Abstractions.Security`
- [x] 1.2 Add `GetByNameAsync(string, CancellationToken)` to `IRoleRepository`
- [x] 1.3 Implement `BCryptPasswordHasher` (cost 12) in `Project.Infrastructure.Security`
- [x] 1.4 Implement `GetByNameAsync` in `RoleRepository` (FirstOrDefaultAsync by Name)
- [x] 1.5 Register `IPasswordHasher` singleton in `DependencyInjection.cs`
- [x] 1.6 `BCryptPasswordHasherTests`: 10 tests covering hash, verify, wrong password, null guards, malformed hash, and salt uniqueness

### Files Changed

| File | Action | Details |
|------|--------|---------|
| `apps/api/src/Project.Application/Abstractions/Security/IPasswordHasher.cs` | Created | Hash + Verify contract |
| `apps/api/src/Project.Application/Abstractions/Persistence/IRoleRepository.cs` | Modified | Added `GetByNameAsync(string, CancellationToken)` |
| `apps/api/src/Project.Infrastructure/Security/BCryptPasswordHasher.cs` | Created | BCrypt cost 12, `EnhancedHashPassword`/`EnhancedVerify` |
| `apps/api/src/Project.Infrastructure/Data/Repositories/RoleRepository.cs` | Modified | `GetByNameAsync` via `FirstOrDefaultAsync(r => r.Name == name)` |
| `apps/api/src/Project.Infrastructure/DependencyInjection.cs` | Modified | `AddSingleton<IPasswordHasher, BCryptPasswordHasher>()` |
| `apps/api/src/Project.Infrastructure/Project.Infrastructure.csproj` | Modified | Added `BCrypt.Net-Next` 4.0.3 |
| `apps/api/tests/Project.UnitTests/Project.UnitTests.csproj` | Modified | Added project reference to `Project.Infrastructure` |
| `apps/api/tests/Project.UnitTests/Security/BCryptPasswordHasherTests.cs` | Created | 10 xUnit tests |

### Verification

```bash
# Unit tests (BCryptPasswordHasher only)
dotnet test Project.UnitTests --filter "FullyQualifiedName~BCryptPasswordHasherTests"
# Result: 10 passed, 0 failed, 0 skipped

# Full unit test suite
dotnet test Project.UnitTests
# Result: 249 passed, 0 failed, 0 skipped (239 existing + 10 new)

# Integration test suite
dotnet test Project.IntegrationTests
# Result: 74 passed, 0 failed, 0 skipped
```

---

## Batch: Phase 2 / PR 2 — Core Seed Components

**Date**: 2026-06-18
**Mode**: Standard (project TDD)
**Work unit**: Seed components + credential validation + catalog + seed logic

### Completed Tasks

- [x] 2.1 Create `SuperadminCredentials` record (Email, PlaintextPassword)
- [x] 2.2 Create `SuperadminCredentialValidator` (presence, length≥12, valid email)
- [x] 2.3 Create `PermissionCatalog` with 22 static entries (users×7, roles×6, permissions×1, menu×5, auth×3)
- [x] 2.4 Create `SeedData.SeedAsync`: idempotent role/permission/user creation via Domain factories, synthetic actorRoles for Superadmin role assignment
- [x] 2.5 `SuperadminCredentialValidatorTests` [Theory]×8 — 19 tests total (adds internal-whitespace rejection)
- [x] 2.6 `PermissionCatalogTests` — forces `PermissionCatalog.All` initialization and validates 22 unique regex-safe keys

### Files Changed

| File | Action | Details |
|------|--------|---------|
| `apps/api/src/Project.MigrationService/SuperadminCredentials.cs` | Created | Record with Email + PlaintextPassword |
| `apps/api/src/Project.MigrationService/SuperadminCredentialValidator.cs` | Created | Static validator: presence, email shape, internal-whitespace rejection, length≥12, email normalization |
| `apps/api/src/Project.MigrationService/PermissionCatalog.cs` | Created | 22 static PermissionKey entries across users(7), roles(6), permissions(1), menu(5), auth(3); all keys now regex-safe |
| `apps/api/src/Project.MigrationService/SeedData.cs` | Created | `SeedAsync` with repository-contract lookups and one tracked `SaveChangesAsync` boundary |
| `apps/api/tests/Project.UnitTests/Project.UnitTests.csproj` | Modified | Added project reference to `Project.MigrationService` |
| `apps/api/tests/Project.UnitTests/MigrationService/SuperadminCredentialValidatorTests.cs` | Created | 19 xUnit tests (1 Fact + 8 Theory) |
| `apps/api/tests/Project.UnitTests/MigrationService/PermissionCatalogTests.cs` | Created | 1 xUnit test covering init, regex validity, uniqueness, and count |

### Implementation Details

**SuperadminCredentialValidator** validates:
- Email presence (null/empty/whitespace → `SUPERADMIN_EMAIL_MISSING`)
- Password presence (null/empty/whitespace → `SUPERADMIN_PASSWORD_MISSING`)
- Email shape (must contain single @, non-empty local & domain parts, domain must have a dot → `SUPERADMIN_EMAIL_INVALID`)
- Password length ≥ 12 chars (→ `SUPERADMIN_PASSWORD_TOO_SHORT`)
- Normalizes email to lowercase on success

**PermissionCatalog** has exactly 22 entries matching the design spec:
- users: read, create, update, deactivate, delete, assignRole, removeRole
- roles: read, create, update, delete, assignPermission, removePermission
- permissions: read
- menu: read, create, update, delete, reorder
- auth: refresh, revokeToken, revokeFamily

**SeedData.SeedAsync** flow:
1. System roles (Superadmin + User) — `GetOrCreateRoleAsync` uses `IRoleRepository` lookups and `Role.Create` when missing
2. Permission catalog — checks existence by `PermissionKey` via `IPermissionRepository`, creates via `Permission.Create` when missing
3. Superadmin role-permission assignments — idempotently adds any missing catalog permissions on every run
4. Superadmin user — checks by email, creates via `User.Create` factory, assigns Superadmin role with synthetic `actorRoles` passed to `User.AssignRole`
5. Single `SaveChangesAsync` commit via `ApplicationDbContext` (minimal Infrastructure-aware boundary; no unit-of-work abstraction exists yet)

### Verification

```bash
# Unit tests (SuperadminCredentialValidator only)
dotnet test Project.UnitTests --filter "FullyQualifiedName~SuperadminCredentialValidatorTests"
# Result: 19 passed, 0 failed, 0 skipped

# Unit tests (PermissionCatalog only)
dotnet test Project.UnitTests --filter "FullyQualifiedName~PermissionCatalogTests"
# Result: 1 passed, 0 failed, 0 skipped

# Full unit test suite
dotnet test Project.UnitTests
# Result: 269 passed, 0 failed, 0 skipped (249 existing + 20 in Phase 2 slice)

# Integration test suite
dotnet test Project.IntegrationTests
# Result: 74 passed, 0 failed, 0 skipped
```

### Deviations from Design

- SeedData now uses repository contracts for entity lookup and retains `ApplicationDbContext` only as the minimal write/commit boundary. There is still no dedicated unit-of-work abstraction, so the single tracked `SaveChangesAsync` call remains the least invasive Infrastructure-aware option.

### Issues Found

None.

### Remaining Tasks

- [ ] 3.1 Create `MigrationWorker` BackgroundService
- [ ] 3.2 Modify `Program.cs` wiring
- [ ] 3.3 Add `BCrypt.Net-Next` to MigrationService.csproj
- [ ] 3.4 `MigrationWorkerTests`
- [ ] 3.5 `SeedDataTests`

### Workload / PR Boundary

- Mode: chained PR slice (feature-branch-chain)
- Current work unit: PR 2 — Core Seed Components
- Boundary: Credential validation, permission catalog, seed logic. No worker, no host wiring, no integration tests.
- Estimated review budget impact: under 400 lines (~300 changed lines for this slice)

### Status

11/16 tasks complete. Ready for Phase 3 / PR 3 (MigrationWorker + wiring + end-to-end tests).
