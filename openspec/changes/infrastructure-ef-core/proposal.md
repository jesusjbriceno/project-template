# Proposal: Infrastructure EF Core

## Intent

Introduce the Infrastructure persistence foundation so Domain/Application contracts can be backed by PostgreSQL through EF Core without leaking EF Core outside Infrastructure. This proposal narrows the first objective to DbContext + schema/mappings working; repositories may be later chained slices.

## Scope

### In Scope
- `ApplicationDbContext`, EF Core registration, DbSet coverage, NoTracking default, split-query behavior, and PostgreSQL mappings.
- Entity configurations/converters for existing Domain entities, value objects, strongly typed IDs, composite junction keys, indexes, audit fields, and soft-delete filters.
- Integration verification with real PostgreSQL/Testcontainers for schema and mapping round-trips.
- Force-chained delivery planning: each implementation PR must stay within the 400 changed-line review budget.

### Out of Scope
- Initial permissions, roles, Superadmin seed data, and migration/bootstrap worker logic.
- Auth/bootstrap, JWT issuance/validation, API endpoints, CQRS handlers, frontend, and product workflows.
- Full repository implementation unless a later chained slice keeps the PR under budget.

## Capabilities

### New Capabilities
- `infrastructure-ef-core`: EF Core persistence capability for PostgreSQL schema, mappings, DbContext configuration, soft-delete query behavior, and integration verification.

### Modified Capabilities
- `application-layer`: Repository/search contracts may define explicit `includeDeleted` parameters where deleted records must be intentionally visible; default behavior remains hidden.

## Approach

Use the exploration’s bottom-up strategy, but constrain the first verifiable slice to DbContext + mappings. Build TDD-first with PostgreSQL integration tests. Soft-deleted records are hidden by default via query filters; explicit repository/search parameters may opt into deleted records. Delivery uses chained PRs; no `size:exception` unless explicitly approved later.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `apps/api/src/Project.Infrastructure/` | New | DbContext, configurations, converters, DI registration |
| `apps/api/tests/Project.IntegrationTests/` | Modified | PostgreSQL mapping/schema tests |
| `openspec/specs/application-layer/` | Modified | Potential explicit include-deleted repository/search contract |
| `openspec/specs/infrastructure-ef-core/` | New | Source spec after archive |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| EF Core/Npgsql preview behavior changes | Med | Pin versions; prove behavior with Testcontainers |
| Mapping mistakes for value objects/composite keys | Med | Round-trip and FK/index integration tests |
| Soft-delete leakage | Med | Default filters plus explicit include-deleted tests |
| PR size over 400 lines | High | Force-chained slices; stop before oversized scope |

## Rollback Plan

Revert the current chained PR slice only. Because this proposal excludes bootstrap migrations/seeding, rollback should remove Infrastructure mappings/tests without data migration rollback. If migrations are added later, rollback must include down migration or database reset guidance.

## Dependencies

- Existing Domain and Application specs/contracts.
- EF Core 10, Npgsql, PostgreSQL 17, Testcontainers.
- `AGENTS.md` Clean Architecture rules and `openspec/config.yaml` strict TDD.

## Success Criteria

- [ ] DbContext resolves through DI against PostgreSQL.
- [ ] All mapped entities round-trip with correct IDs/value objects/audit fields.
- [ ] Soft-deleted records are hidden by default and explicitly includable where specified.
- [ ] Integration tests pass with real PostgreSQL.
- [ ] Planned implementation slices stay within 400 changed lines each.
