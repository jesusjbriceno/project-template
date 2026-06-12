# Exploration: domain-model

## Current State

The project has completed Phases 0+1 (monorepo scaffold, Docker Compose, health endpoint, integration test). The Clean Architecture layers exist as empty shell projects:

- `Project.Domain/` — empty (only `Project.Domain.csproj` with net10.0, Nullable enabled, no dependencies)
- `Project.Application/` — empty (only `.csproj` referencing Domain + FluentValidation)
- `Project.Infrastructure/` — empty (only `.csproj` referencing Application + EF Core + Npgsql)
- `Project.UnitTests/` — empty (only `.csproj` referencing Domain + Application, xUnit v2.9.3, coverlet)

No C# source files exist in Domain, Application, or Infrastructure. The only existing C# files are `Program.cs` (API scaffold with health endpoint), `Program.cs` (MigrationService scaffold), and `HealthEndpointTests.cs` (2 integration tests).

OpenSpec is configured at `openspec/config.yaml` with `strict_tdd: true` and `test_command: "dotnet test apps/api"`.

**Gap to fill:** The Domain layer — the core of Clean Architecture — has zero entities, value objects, or domain rules. Phase 2 must establish the foundational domain model that all upper layers depend on.

## Affected Areas

- `apps/api/src/Project.Domain/` — **primary target**: all new entity/value object classes
- `apps/api/tests/Project.UnitTests/` — **co-target**: unit tests for all domain objects (TDD: tests first)
- `apps/api/src/Project.Application/` — **depends on Domain** (already referenced via .csproj); no changes needed in this phase unless interfaces leak upward, but Domain must be stable before Application begins
- `openspec/specs/` — will receive delta specs in the subsequent `sdd-spec` phase for the `auth` domain
- No existing code is modified — this is a greenfield addition to an empty layer

## Domain Entities and Value Objects

Based on the project's documented auth/RBAC requirements and the entities listed in the exploration scope:

### Value Objects

| Value Object | Purpose | Key Rules |
|---|---|---|
| **Email** | User email address | Normalized lowercase, valid RFC 5322 format, equality by value, implicit conversion to/from string |
| **PermissionKey** | Granular permission identifier | Format `action.resource` (e.g., `users.create`, `roles.read`), lowercase, dot-separated, validated pattern |

### Entities

| Entity | Purpose | Key Invariants |
|---|---|---|
| **User** | System user identity | Has Email value object, IsActive flag, security stamp, password hash (opaque to domain), audit fields (CreatedAt/UpdatedAt/LastLoginAt as DateTimeOffset), collection of UserRoles |
| **Role** | Named role grouping permissions | Has Name, IsSystem (protected roles cannot be deleted), Description, collection of RolePermissions |
| **Permission** | Single grantable action | Has PermissionKey value object, Description, optional Category for UI grouping |
| **UserRole** | Many-to-many User↔Role junction | Composite identity (UserId + RoleId), AssignedAt/AssignedBy audit |
| **RolePermission** | Many-to-many Role↔Permission junction | Composite identity (RoleId + PermissionId) |
| **RefreshToken** | JWT refresh token record | Token is **hashed** (SHA-256) server-side, Includes ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash for rotation chain, Domain methods: `IsExpired()`, `IsRevoked()`, `IsActive()`, `Revoke(replacedByTokenHash)` |
| **MenuItem** | Navigation menu entry | Label, Icon, Route, optional parent-child hierarchy (ParentId), SortOrder, IsVisible, optional PermissionKey for visibility gating |

### Domain Rules (Identified)

1. **RBAC model**: Roles contain Permissions via RolePermission; Users are assigned Roles via UserRole.
2. **Granular permissions**: PermissionKey format is `action.resource` — single responsibility per key.
3. **System role protection**: Roles with `IsSystem = true` MUST NOT be deletable by any user. Attempting to delete a system role throws a domain exception.
4. **Last superadmin guard**: The system SHALL NOT allow removal of the last user with the SuperAdmin role. This is a domain invariant enforced at the UserRole/User level (meaning the delete-user use case in Application will need to check this; the Domain can express this as a guard method).
5. **Refresh token security**: Tokens are stored as SHA-256 hashes (never raw), rotated on use (old token revoked, new token issued with `ReplacedByTokenHash`), and reuse detection means if a revoked token is presented, the entire token family/chain is revoked.
6. **MenuItem hierarchy**: Menu items support recursive parent-child nesting via `ParentId` self-reference. Circular references MUST be prevented (domain guard or application-level validation).
7. **Email uniqueness**: Email case-insensitive uniqueness is a persistence concern (Infrastructure), but the Email value object normalizes to lowercase.
8. **Audit fields**: All entities use `DateTimeOffset` (never `DateTime`), per project convention.
9. **Password hash**: Domain stores the hash string but does NOT perform hashing — that is an Application/Infrastructure concern (bcrypt). Domain treats password hash as an opaque string.
10. **Soft delete vs hard delete**: Phase 2 decision needed. Users should likely be soft-deleted (IsActive = false) rather than hard-deleted to preserve referential integrity. RefreshTokens CAN be hard-deleted after expiry for cleanup.

### Boundaries: What is NOT in Domain (Phase 2)

| Concern | Layer | Why Not in Domain |
|---|---|---|
| EF Core DbContext, entity configurations, migrations | Infrastructure | EF Core references violate Clean Architecture |
| Repository interfaces | Application | Convention: interfaces defined in Application, not Domain |
| CQRS commands/queries/handlers | Application | Orchestration logic, not domain rules |
| FluentValidation validators | Application | Input validation for commands, separate from domain invariants |
| Controllers / Minimal API endpoints | API | HTTP concern |
| JWT generation, auth middleware | API / Infrastructure | Infrastructure concern |
| Superadmin seeding | MigrationService | Infrastructure concern (future phase) |
| DTOs, response models | Application / API | Data transfer, not domain logic |
| Password hashing (bcrypt) | Application / Infrastructure | Infrastructure/security concern; Domain stores hash only |
| `Result<T>` pattern types | Application | Orchestration pattern, not domain rules (Domain throws domain exceptions or uses guard clauses) |

## Approaches

### Approach 1: Value Objects First, Entities Second (RECOMMENDED)

Build bottom-up: value objects (Email, PermissionKey) → entities with no cross-references → junction entities → entity with collections (User with UserRoles, Role with RolePermissions).

- **Pros**: Each class testable in isolation, incremental complexity, clean TDD flow (red-green-refactor per class), avoids circular dependency confusion
- **Cons**: Slightly more files upfront before "visible" domain works end-to-end
- **Effort**: Medium

### Approach 2: Entity-First with Embedded Value Logic

Define entities with inline value semantics (Email as a validated string property on User, PermissionKey as a string on Permission) and extract value objects later if needed.

- **Pros**: Fewer files, faster to get entities visible
- **Cons**: Violates value object pattern, loses type safety (Email is just a string, losing domain meaning), harder to enforce validation in all call sites, tech debt to refactor later
- **Effort**: Low

### Approach 3: Aggregate-Root-Centric (DDD Tactical)

Group into aggregates: User aggregate root owns UserRoles; Role aggregate root owns RolePermissions. Enforce consistency boundaries at aggregate level.

- **Pros**: Richer domain model, stronger consistency guarantees, DDD-aligned
- **Cons**: Premature for Phase 2 — aggregates work best when you have clear transaction boundaries from use cases; adds complexity without immediate benefit; CQRS + repositories aren't built yet
- **Effort**: High

## Recommendation

**Approach 1: Value Objects First, Entities Second.**

This aligns with TDD (one class at a time), Clean Architecture (Domain has zero dependencies), and the project convention of small atomic methods. Value objects like `Email` and `PermissionKey` give immediate type safety benefits. Entities can reference them from day one. The domain can evolve into aggregates later when Application-layer use cases define transaction boundaries.

**TDD sequence (recommended order):**
1. `Email` value object + tests
2. `PermissionKey` value object + tests
3. `Permission` entity + tests
4. `Role` entity + tests
5. `RolePermission` junction + tests
6. `User` entity + tests
7. `UserRole` junction + tests
8. `RefreshToken` entity + tests
9. `MenuItem` entity + tests

Each step: write failing unit test → implement minimal domain class → make test pass → refactor.

## Risks

- **No existing C# conventions to follow**: The Domain project has zero source files. Naming patterns, folder structure (flat vs. subdirectories), and exception types must be established from scratch and documented for consistency.
- **Junction entity design ambiguity**: `UserRole` and `RolePermission` are many-to-many relationships. In a persistence-ignorant domain, these should be plain entities with no EF Core navigation properties. But the team must agree: are they simple value-holding classes or do they carry domain behavior?
- **RefreshToken domain complexity**: Token reuse detection logic (revoking the entire chain) is security-sensitive. Getting this wrong in Domain affects auth integrity.
- **MenuItem circular reference**: Self-referencing hierarchy with ParentId requires a cycle-detection guard. This could be a domain method or deferred to Application — design decision needed.
- **"Last superadmin cannot be deleted"**: This invariant requires knowledge of other Users (a cross-aggregate check), which blurs the single-entity boundary. It may need to be enforced at the Application layer rather than purely in Domain.

## Test Scenarios to Spec

### Email Value Object
- Creation with valid email succeeds
- Creation with null/empty/whitespace throws
- Creation with invalid format throws
- Equality: two Emails with same normalized value are equal
- Implicit conversion to string returns normalized value
- Explicit conversion from string validates

### PermissionKey Value Object
- Creation with valid `action.resource` format succeeds
- Creation with null/empty throws
- Creation with uppercase, spaces, or invalid characters throws
- Equality: same key string → equal
- ToString() returns the dot-separated key

### User Entity
- Creation with valid Email succeeds, sets IsActive=true, sets CreatedAt
- Deactivation sets IsActive=false, sets UpdatedAt
- Login updates LastLoginAt
- Security stamp regeneration produces new value
- Cannot be created with null Email

### Role Entity
- Creation with valid Name succeeds, sets IsSystem=false
- Adding a Permission via RolePermission succeeds
- Removing a Permission succeeds
- System role deletion guard: attempting to mark IsSystem role for deletion signals domain violation

### Permission Entity
- Creation with valid PermissionKey succeeds
- Description is optional
- Category groups permissions logically

### RefreshToken Entity
- New token is active (IsActive = true)
- Token expires: IsExpired() returns true after ExpiresAt
- Revocation: IsRevoked() returns true after Revoke(), IsActive() returns false
- Rotation: Revoke(replacedByTokenHash) sets RevokedAt and ReplacedByTokenHash
- Reuse detection: presenting an already-revoked token should be detectable (domain method or guard)

### MenuItem Entity
- Creation with Label succeeds
- Parent-child hierarchy: child references parent via ParentId
- SortOrder determines display position
- IsVisible controls UI rendering
- Circular reference prevention: cannot set ParentId to a descendant

## Ready for Proposal

**Yes.** The exploration has identified:
- Exact domain entities and value objects needed
- Clear domain rules and invariants
- What is in-scope vs. out-of-scope for Domain
- Recommended approach and TDD sequence
- Test scenarios for unit tests
- Risks and open design decisions

The orchestrator should launch `sdd-propose` next to create a formal change proposal with scope, approach, and rollback plan. The proposal should reference this exploration.

### Open Decisions for Proposal Phase

1. Folder structure: flat `Project.Domain/` root vs. subdirectories (`Entities/`, `ValueObjects/`, `Exceptions/`)?
2. Domain exception approach: custom exception hierarchy (`DomainException` base) or `ArgumentException`/`InvalidOperationException`?
3. `Guid` vs strongly-typed IDs (e.g., `UserId`, `RoleId` record structs)? Strongly-typed IDs prevent accidental parameter swapping.
4. Junction entities: pure data holders or do they carry behavior (e.g., `UserRole.Assign()` factory method)?
5. Where does "last superadmin guard" live? Domain guard method on User entity or Application-layer check?
6. MenuItem cycle detection: domain-level guard or application-level validation?
