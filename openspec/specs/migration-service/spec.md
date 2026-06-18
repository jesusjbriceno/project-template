# migration-service Specification

## Purpose

Database bootstrap: applies pending EF Core migrations, seeds RBAC data, creates admin access, then exits.

## Requirements

### Requirement: Migration Orchestration

Pending EF Core migrations MUST be applied via `MigrateAsync()` with execution-strategy retry, then seed invoked, then host stopped. Migration failure SHALL block seed and exit non-zero.

#### Scenario: Happy path

- GIVEN no migrations applied and valid Superadmin credentials
- WHEN MigrationWorker runs
- THEN migrations applied, seed completes, host exits code 0

#### Scenario: Migration failure blocks seed

- GIVEN a broken migration script
- WHEN MigrationWorker runs
- THEN seed is NOT invoked and host exits non-zero

### Requirement: Superadmin Credential Validation

`SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD` MUST be present in configuration. Either missing or whitespace-only SHALL abort the process before any seed operation. Credentials SHALL NOT appear in plaintext logs.

#### Scenario: Missing credential aborts

- GIVEN either `SUPERADMIN_EMAIL` or `SUPERADMIN_PASSWORD` is unset or whitespace
- WHEN seed validation runs
- THEN process exits non-zero with a log message identifying the missing variable

### Requirement: System Role Seeding

Roles `Superadmin` and `User` (IsSystem=true) SHALL be created idempotently — checked by role name before insert. Existing roles MUST NOT be modified on re-run.

#### Scenario: First run creates both roles

- GIVEN empty roles table
- WHEN seed runs
- THEN `Superadmin` and `User` exist, both with IsSystem=true

#### Scenario: Re-run preserves existing roles

- GIVEN both system roles exist
- WHEN seed runs again
- THEN no duplicates created, no existing role properties mutated

### Requirement: Permission Catalog Seeding

Current-state permission catalog SHALL be seeded by existence check on PermissionKey. The catalog MUST NOT include unimplemented future permissions. Each permission SHALL be created only once.

(Open design: exact `action.resource` key list defined in design phase.)

#### Scenario: Permissions seeded and idempotent

- GIVEN empty or populated permissions table
- WHEN seed runs
- THEN all defined permissions exist with unique keys; re-run creates no duplicates

### Requirement: Superadmin Account Creation

One Superadmin user SHALL be created from `SUPERADMIN_EMAIL` and hashed `SUPERADMIN_PASSWORD`. Password hashing MUST use a production-grade algorithm (BCrypt or Argon2id — resolved in design phase). The Superadmin role SHALL be assigned using a synthetic bootstrap context (passing the Superadmin role in `actorRoles`) to satisfy the Domain's `User.AssignRole()` guard. User created only if email does not already exist.

#### Scenario: First seed creates Superadmin

- GIVEN no user with configured email exists
- WHEN seed runs
- THEN user exists with hashed password and Superadmin role assignment

#### Scenario: Existing Superadmin unchanged on re-run

- GIVEN Superadmin user already exists with configured email
- WHEN seed runs again
- THEN password, roles, and properties are unchanged

### Requirement: Seed Idempotence

Schema migrations are safe to re-run via EF Core migration history. Seed data MUST NOT silently repair, mutate, or duplicate existing roles, permissions, or Superadmin state. Future data changes SHALL require explicit controlled migrations or data seeds.

#### Scenario: Full idempotency — second run is a no-op

- GIVEN a database where seed completed successfully
- WHEN MigrationWorker runs again
- THEN no entities are added, modified, or duplicated across roles, permissions, and Superadmin

### Requirement: Clean Architecture and Security

The seed component MUST use Domain `Create()` factory methods and Application repository contracts. Bypassing Domain invariants or using EF Core directly for entity construction is forbidden. Passwords and connection strings MUST NOT be hardcoded or logged.

#### Scenario: Factory-only entity creation

- WHEN seed creates any entity
- THEN only Domain factory methods are invoked; no `new Entity()` or raw EF Core state manipulation occurs

### Requirement: Integration Test Verification

PostgreSQL 17 via Testcontainers SHALL verify: migration application, seed correctness (roles, permissions, Superadmin), full idempotency, and credential validation failure (missing credentials abort the process).

#### Scenario: Testcontainers integration test cycle

- GIVEN a fresh PostgreSQL 17 container
- WHEN the test suite executes against MigrationService
- THEN migrations apply, seed data is correct, re-run is idempotent, and missing credentials abort
