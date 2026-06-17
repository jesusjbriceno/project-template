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

### Deviations from Design

None — implementation matches design.

### Issues Found

None.

### Remaining Tasks

- [ ] 2.1 Create `SuperadminCredentials` record
- [ ] 2.2 Create `SuperadminCredentialValidator`
- [ ] 2.3 Create `PermissionCatalog`
- [ ] 2.4 Create `SeedData.SeedAsync`
- [ ] 2.5 `SuperadminCredentialValidatorTests`
- [ ] 3.1 Create `MigrationWorker`
- [ ] 3.2 Modify `Program.cs` wiring
- [ ] 3.3 Add `BCrypt.Net-Next` to MigrationService.csproj
- [ ] 3.4 `MigrationWorkerTests`
- [ ] 3.5 `SeedDataTests`

### Workload / PR Boundary

- Mode: chained PR slice (feature-branch-chain)
- Current work unit: PR 1 — Foundation
- Boundary: Contracts + hasher + role lookup only. No seed components, no worker.
- Estimated review budget impact: well under 400 lines (~140 changed lines for this slice)
