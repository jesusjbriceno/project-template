# infrastructure-ef-core Specification

## Purpose

EF Core persistence for PostgreSQL — DbContext, entity mappings, value converters, soft-delete filters, audit timestamps. EF Core confined to Infrastructure per Clean Architecture.

## Requirements

### Requirement: Clean Architecture Boundary

EF Core types (DbContext, DbSet, IEntityTypeConfiguration) MUST only exist in Infrastructure. Domain and Application SHALL NOT reference EF Core, Npgsql, or IQueryable.

#### Scenario: Layer isolation

- GIVEN `Project.Infrastructure.csproj` references EF Core + Npgsql
- WHEN `dotnet build apps/api` is run
- THEN Domain and Application compile with zero EF Core assembly references

### Requirement: ApplicationDbContext Configuration

| Config | Value |
|--------|-------|
| QueryTrackingBehavior | NoTracking |
| QuerySplittingBehavior | SplitQuery |
| ExecutionStrategy | NpgsqlRetryingExecutionStrategy (3 retries) |
| Provider | Npgsql / PostgreSQL 17 |
| Soft-delete filter | Per-entity: `e => e.IsDeleted == false` for User, Role, MenuItem only |

#### Scenario: NoTracking by default

- GIVEN a read query via DbContext without `.AsTracking()`
- WHEN entities are retrieved
- THEN change tracker SHALL NOT track them; `SaveChangesAsync` produces no updates

#### Scenario: Split queries for navigation

- GIVEN a query with multiple `.Include()` calls on collection navigations
- WHEN executed against PostgreSQL
- THEN EF Core SHALL split into separate SQL queries (no cartesian explosion)

### Requirement: Entity Configurations

| Entity | Table | PK | Key Indexes / FKs | Soft-Delete |
|--------|-------|----|--------------------|-------------|
| User | users | UserId (HasConversion) | Email unique | Yes |
| Role | roles | RoleId | Name unique | Yes |
| Permission | permissions | PermissionId | Key unique | No |
| RefreshToken | refresh_tokens | RefreshTokenId | TokenHash unique, FamilyId, ExpiresAt | No |
| MenuItem | menu_items | MenuItemId | ParentId self-ref FK | Yes |
| UserRole | user_roles | Composite {UserId, RoleId} | FK→User, FK→Role | N/A |
| RolePermission | role_permissions | Composite {RoleId, PermissionId} | FK→Role, FK→Permission | N/A |

#### Scenario: Mapping round-trip

- GIVEN any entity saved via DbContext
- WHEN retrieved by ID in a new DbContext instance against real PostgreSQL
- THEN all properties match original values including value objects and audit timestamps

### Requirement: ID and Value Object Mappings

Strongly-typed IDs SHALL use `HasConversion(id => id.Value, g => XxxId.From(g))` with a ValueComparer per ID type.

| VO | Storage | Converter |
|----|---------|-----------|
| Email | text | `string ↔ Email` (normalize) |
| PermissionKey | text | `string ↔ PermissionKey` |
| DeletionPolicy | jsonb | System.Text.Json |

#### Scenario: ID and VO round-trip

- GIVEN UserId(Guid) and Email("User@Example.com")
- WHEN saved and retrieved from PostgreSQL
- THEN stored as `uuid`/`text`, retrieved as `UserId`/normalized `"user@example.com"`

### Requirement: Soft-Delete Filter

Entities with soft-delete (User, Role, MenuItem) SHALL have query filter `e => e.IsDeleted == false` applied per-entity in `IEntityTypeConfiguration.Configure`. Permission and RefreshToken SHALL NOT have this filter.

#### Scenario: Hidden by default

- GIVEN a soft-deleted User in the database
- WHEN queried via `dbContext.Users.ToListAsync()`
- THEN soft-deleted User SHALL NOT appear

### Requirement: Audit Timestamps

CreatedAt and UpdatedAt SHALL be set automatically. CreatedAt on add; UpdatedAt on add and update. Both SHALL use `DateTimeOffset`.

#### Scenario: Auto timestamps

- GIVEN a new User is added via DbContext
- WHEN `SaveChangesAsync` completes
- THEN `CreatedAt` and `UpdatedAt` are non-default DateTimeOffset values

### Requirement: Composite Keys

Junction tables (UserRole, RolePermission) SHALL use composite primary keys via `HasKey(e => new { e.UserId, e.RoleId })` with shadow-property FK conversions. No synthetic surrogate keys.

#### Scenario: Junction insert uniqueness

- GIVEN a UserRole {UserId=A, RoleId=B}
- WHEN inserted into PostgreSQL
- THEN a second insert with same {A, B} violates unique constraint

### Requirement: Integration Test Verification

All mappings MUST pass integration tests against PostgreSQL 17 via Testcontainers covering: CRUD round-trips, soft-delete exclusion/inclusion, composite key uniqueness, and audit timestamp generation.

#### Scenario: Testcontainers verification

- GIVEN PostgreSQL 17 container via Testcontainers
- WHEN CRUD, filter, and constraint tests run
- THEN all entity mappings pass against real PostgreSQL
