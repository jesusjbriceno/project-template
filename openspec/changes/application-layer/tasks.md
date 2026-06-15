# Tasks: Application Layer Foundation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 1,300-1,500 including SDD artifacts; Work Unit 2 alone is 739 new-file lines before modified files |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1a → PR 1b |
| Delivery strategy | force-chained |
| Chain strategy | feature-branch-chain |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Result pattern + CQRS contracts + tests | PR 1a | base=`feature/application-layer` tracker; ~458 code/test lines |
| 2 | Repository + Security interfaces + tests | PR 1b | base=PR 1a branch; 739 new-file lines before modified files; depends on Result/CQRS types |

## Phase 1: Result Pattern & CQRS Contracts

- [x] 1.1 Write `tests/.../Common/ResultTests.cs` + `ErrorTests.cs` — failing tests for `Result`, `Result<T>`, `Error`
- [x] 1.2 Create `src/Project.Application/Common/Error.cs` with machine-readable error code/message contract
- [x] 1.3 Create `src/Project.Application/Common/Result.cs` + `ResultT.cs` with `IsSuccess`, implicit conversions
- [x] 1.4 Create `src/Project.Application/GlobalUsings.cs`
- [x] 1.5 Write `tests/.../Messaging/CqrsContractTests.cs` — compile-time contract proof
- [x] 1.6 Create `src/.../Abstractions/Messaging/{ICommand,IQuery,ICommandHandler,IQueryHandler}.cs`

## Phase 2: Repository & Security Interfaces

- [x] 2.1 Write `tests/.../Persistence/RepositoryContractTests.cs` — hand-rolled stubs prove async sigs
- [x] 2.2 Create `src/.../Abstractions/Persistence/{IUser,IRole,IPermission,IRefreshToken,IMenuItem}Repository.cs`
- [x] 2.3 Write `tests/.../Security/SecurityBoundaryTests.cs` — two-interface distinction, no raw-token surface
- [x] 2.4 Create `src/.../Abstractions/Security/{IUserSession,ISuperadminEnforcementContext,ITokenService}.cs`
- [x] 2.5 Create `src/.../Abstractions/Validation/IValidated.cs`
- [x] 2.6 Create `src/.../Abstractions/Persistence/IBaseRepository.cs` with `IBaseRepository<TEntity,TId>` — shared CRUD + paginated search
- [x] 2.7 Create `src/.../Abstractions/Persistence/{PageRequest,PagedResult}.cs` — Application-level pagination types
- [x] 2.8 Refactor per-aggregate repositories to inherit from `IBaseRepository<TEntity,TId>` — remove redundant methods
- [x] 2.9 Write `tests/.../Persistence/{PageRequest,PagedResult}Tests.cs` — TDD for pagination types
- [x] 2.10 Write `tests/.../Persistence/BaseRepositoryContractTests.cs` — compile-time proof for base repo (within RepositoryContractTests)

## Phase 3: Verification

- [x] 3.1 `dotnet build apps/api` — zero EF Core / ASP.NET references in Application project
- [x] 3.2 `dotnet test apps/api` — all tests green
- [x] 3.3 Update `ROADMAP.md` Backend Application row to "In Progress"
