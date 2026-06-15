# Proposal: Application Layer Foundation

## Intent

Create the first Application-layer slice so future use cases have stable Clean Architecture contracts for Result-based outcomes, CQRS, repositories, validation, auth/session boundaries, and controlled Superadmin invariant enforcement. This implements the roadmap’s Backend Application foundation while respecting `AGENTS.md` and `openspec/config.yaml`: TDD, no EF Core in Application, no AutoMapper, Result pattern, CQRS, FluentValidation, OWASP-aware auth/session design.

## Scope

### In Scope
- Result pattern foundation: `Result`, `Result<T>`, application errors/codes.
- CQRS command/query and handler contracts without external mediator dependency.
- Application-facing repository interfaces for Domain aggregates: shared `IBaseRepository<TEntity,TId>` with CRUD + paginated search, plus per-aggregate interfaces (`IUserRepository`, `IRoleRepository`, `IPermissionRepository`, `IRefreshTokenRepository`, `IMenuItemRepository`).
- Generic auth/session/security contracts, including refresh-token family revocation and login/logout/session lifecycle boundaries.
- Controlled Superadmin enforcement context so generic contracts cannot bypass Domain invariants.
- Foundation unit tests for Result behavior and contract expectations where applicable.

### Out of Scope
- Concrete use-case handlers, except minimal contract proof if required.
- Infrastructure EF Core implementations, migrations, API controllers/endpoints.
- Concrete JWT issuance/validation, login/logout flows, or refresh-token persistence.
- MediatR adoption unless a later design explicitly justifies the dependency.

## Capabilities

### New Capabilities
- `application-layer`: Application boundary contracts for Result/CQRS, repositories, validation, auth/session lifecycle, refresh-token family revocation, and controlled Superadmin enforcement.

### Modified Capabilities
- None. `domain-model` requirements remain unchanged; Application consumes and orchestrates existing invariants.

## Approach

Use the exploration’s foundation-first approach. Define small, explicit contracts in `Project.Application`, backed by TDD in `Project.ApplicationTests`. Keep repository interfaces domain-specific, keep EF Core out, prefer custom CQRS contracts now, and model auth/security as interfaces so Infrastructure/API can implement them later.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `apps/api/src/Project.Application/` | New | Result, CQRS, repositories, validation/security contracts. |
| `apps/api/tests/Project.ApplicationTests/` | New | Foundation unit tests. |
| `openspec/specs/domain-model/spec.md` | Reference | Superadmin and refresh-token invariants consumed unchanged. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Result API lock-in | Med | Keep minimal, test common success/failure flows. |
| Generic security contracts bypass invariants | Med | Require specific Superadmin enforcement context boundary. |
| Auth scope grows too large | Med | Define contracts only; defer concrete JWT/login flows. |

## Rollback Plan

Revert the Application foundation slice and its tests. No persistence, API, or Domain behavior changes are introduced.

## Dependencies

- `Project.Domain` completed invariants.
- Existing `FluentValidation` reference.

## Success Criteria

- [ ] Application contracts compile without EF Core or API references.
- [ ] Foundation tests pass under `dotnet test apps/api`.
- [ ] New spec capability can drive design/tasks for the first chained PR.
