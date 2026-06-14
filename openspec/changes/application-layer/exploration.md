# Exploration: Application Layer Foundation

## Current State

The `Project.Application` project exists as an empty .csproj scaffold. It references `Project.Domain` and `FluentValidation 12.1.1` but contains **zero `.cs` source files** — only build artifacts in `obj/` and `bin/`. The companion test project `Project.ApplicationTests` is also empty (xUnit scaffold, no test files). The `Project.Infrastructure` project is equally empty.

Meanwhile, the Domain layer is fully implemented and verified (27 `.cs` files, 241 green tests, 14/14 spec scenarios covered). It exposes these Application-layer coupling points:

| Domain type | What Application must provide |
|---|---|
| `User.Deactivate()` | `IReadOnlyCollection<User> activeSuperadmins` — pre-loaded by Application |
| `User.AssignRole()` | `IReadOnlyCollection<Role>? actorRoles` — pre-loaded by Application for superadmin gate |
| `User.RemoveRole()` | `IReadOnlyCollection<User> activeSuperadmins` — pre-loaded by Application |
| `User.Delete()` | `IReadOnlyCollection<User> activeSuperadmins` — pre-loaded by Application |
| `MenuItem.SetParent()` | `IReadOnlyCollection<MenuItem> allItems` — pre-loaded by Application for cycle detection |
| `RefreshToken.Rotate()` | `RefreshTokenReuseSignalException` — Application must catch and trigger family revocation |
| All entity factories | `IClock clock` — Application must inject `SystemClock` or test stub |
| `AuditableEntity.MarkCreated/Updated/Deleted()` | `createdBy`/`updatedBy`/`deletedBy` string — Application must supply caller identity |

The `Program.cs` composition root is a scaffold (`app.MapHealthChecks("/health")` only) with placeholder comments for future DI registration.

## Affected Areas

- `apps/api/src/Project.Application/` — **empty**, the entire layer must be built from scratch
- `apps/api/tests/Project.ApplicationTests/` — **empty**, tests must be built alongside foundation code
- `apps/api/src/Project.Api.Controllers/Program.cs` — DI registration comments reference future Application services (no code today)
- `apps/api/src/Project.Domain/` — **complete**, no changes needed; Application consumes Domain as-is
- `openspec/specs/domain-model/spec.md` — reference for invariants Application must enforce transactionally
- `AGENTS.md` — layer rules, Result pattern mandate, CQRS mandate, FluentValidation mandate, no-AutoMapper constraint, no-EF-in-Application constraint

## Approaches

### Approach A: Full-scope monolithic Application layer

Build Result pattern, CQRS abstractions, all repository interfaces, FluentValidation pipeline, AND all use-case handlers in a single PR.

| Pros | Cons | Effort |
|------|------|--------|
| Complete Application layer in one delivery | **Massively exceeds 400-line review budget** (est. 2000-3500 lines) | High |
| No orchestration overhead between slices | High risk of rework if design flaws found late | |
| | Review fatigue; impossible to give meaningful feedback | |
| | Violates force-chained delivery strategy | |

### Approach B: Foundation-first incremental (RECOMMENDED)

**Slice 1 — Foundation**: Result pattern, CQRS contracts, repository interfaces, Superadmin enforcement boundary contract, FluentValidation pipeline contract. **Estimated: 250-350 lines.**

Followed by incremental use-case slices:
- Slice 2: User use cases (create, deactivate, assign/remove role, delete)
- Slice 3: Auth use cases (login, token rotation, token revocation, family revocation)
- Slice 4: RBAC use cases (role CRUD, permission assignment, permission catalog)
- Slice 5: Menu use cases (CRUD, hierarchy, visibility)

| Pros | Cons | Effort |
|------|------|--------|
| Each slice fits 400-line review budget | More slices to orchestrate | Low per slice |
| Foundation is reviewable as a standalone architectural unit | Subsequent slices depend on foundation merge | |
| Aligned with force-chained delivery | | |
| Design can be course-corrected after foundation review | | |
| Tests travel with foundations (TDD-compliant) | | |

### Approach C: Use-case-driven ad-hoc emergence

Skip explicit foundation slice. Define Result/CQRS/repo interfaces inline as each use-case group is built.

| Pros | Cons | Effort |
|------|------|--------|
| Each slice is fully self-contained | Risk of inconsistent patterns across slices | Medium |
| No blocking dependency on foundation merge | Repository interfaces defined in multiple places | |
| | CQRS contracts may diverge | |
| | Harder to review architectural consistency | |
| | Refactoring tax when patterns need realignment | |

## Recommendation

**Approach B — Foundation-first incremental.** The Application layer is an architectural boundary that deserves deliberate design before use cases pile on top. A **250-350 line foundation slice** is small enough to review thoroughly and establishes the contracts every subsequent use-case slice will obey. This aligns with the 400-line review budget and force-chained delivery strategy.

### Foundation slice scope

| Component | Lines (est.) | Details |
|---|---|---|
| `Result` / `Result<T>` | 60-80 | Generic result pattern with success/failure, error codes, implicit conversions. No exceptions for control flow. |
| CQRS contracts | 30-40 | `ICommand`, `IQuery<TResult>`, `ICommandHandler<TCommand>`, `IQueryHandler<TQuery, TResult>` — marker/contract interfaces only |
| Repository interfaces | 80-120 | `IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IMenuItemRepository` — async CRUD + query methods matching domain entity APIs |
| Validation pipeline contract | 20-30 | Optional in foundation: `IValidated` marker or FluentValidation pipeline behavior contract. Validators themselves belong with use cases. |
| Superadmin enforcement boundary | 40-60 | Interface or static helper to define the pre-loading contract: `ISuperadminContext` with methods to fetch active superadmins, actor roles. Defines the pattern without Infrastructure implementation. |
| `GlobalUsings.cs` | 10-15 | Shared usings for the Application project |
| Foundation unit tests | 80-100 | `Result` tests, contract interface validation, TDD baseline |
| **Total estimate** | **280-420** | Within 400-line budget with tight factoring |

### What stays OUT of scope until Infrastructure/API phases

- EF Core DbContext, entity configurations, PostgreSQL type mappings
- Repository implementations (Infrastructure concern)
- Migration creation (Infrastructure/MigrationService concern)
- FluentValidation validators for specific commands (belong with use-case slices)
- Auth endpoints, controller actions (API concern)
- ProblemDetails/Result HTTP mapping (API concern)
- MediatR NuGet package decision (can be deferred: custom CQRS dispatcher works; MediatR can layer on top later)

## Risks

- **Result pattern design lock-in**: Once `Result<T>` is defined, all handlers must return it. Getting the signature wrong forces a refactor across every subsequent slice. Mitigation: review the Result pattern against known .NET conventions (FluentResults library, Ardalis.Result, custom) before committing.
- **Repository interface granularity**: Too coarse (one `IRepository<T>`) loses domain semantics; too fine (per-method interfaces) creates interface explosion. Per-entity interfaces balance both. Mitigation: review the domain entity API surface to ensure repository methods match real use-case needs.
- **Superadmin pre-loading becomes a cross-cutting concern**: Every user mutation needs `activeSuperadmins` loaded. If the boundary is unclear, use cases will duplicate loading logic. Mitigation: define `ISuperadminContext` in foundation so all use cases follow the same contract.
- **RefreshToken family revocation complexity**: `RefreshTokenReuseSignalException` triggers revocation of ALL tokens in a family. The Application layer must catch this and orchestrate a batch revocation — a non-trivial coordination. Mitigation: define the family-revocation contract (`IRevokeTokenFamily`) now, implement in the auth slice.
- **No Infrastructure yet**: Repository interfaces have no implementations. Unit tests must mock interfaces. Integration tests require Infrastructure (future phase). Mitigation: this is by design — Clean Architecture layers build bottom-up. TDD still works with interface stubs.

## Slicing Plan for Force-Chained Delivery

```
develop
  └── feature/application-layer-01-foundation   ← PR #1 (this exploration's target)
        └── feature/application-layer-02-users   ← PR #2 (targets #1)
              └── feature/application-layer-03-auth  ← PR #3 (targets #2)
                    └── feature/application-layer-04-rbac  ← PR #4 (targets #3)
                          └── feature/application-layer-05-menu  ← PR #5 (targets #4)
```

Each PR is ~250-400 changed lines, independently verifiable, with its own tests.

## Suggested First Proposal Scope

The first proposal (`sdd-propose`) should cover **only the Foundation slice**:
1. Result pattern (`Result`, `Result<T>`)
2. CQRS contracts (`ICommand`, `IQuery<T>`, `ICommandHandler`, `IQueryHandler`)
3. Repository interfaces (`IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IMenuItemRepository`)
4. Superadmin enforcement boundary (`ISuperadminEnforcementContext` or documented pattern)
5. Application `GlobalUsings.cs`
6. Foundation unit tests (TDD: Result, contract validation)

## Ready for Proposal

**Yes.** The Domain layer is complete and stable. The Application layer has zero existing code — no migration risk, no coupling to untangle. The foundation scope is well-bounded and fits the 400-line review budget. The next step is `sdd-propose` for the foundation slice.
