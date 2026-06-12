# Design: Domain Model

## Technical Approach

`Project.Domain` is a pure, persistence-agnostic .NET 10 class library (`Microsoft.NET.Sdk`, net10.0, `Nullable` enabled, zero package references). TDD order revised: strongly typed ID primitives → value objects → entities → junctions → security tokens → menu hierarchy. Domain expresses invariants through guarded methods throwing typed `DomainException` derivatives; cross-entity invariants take sibling state as parameters so Domain stays unit-testable without a database.

## Architecture Decisions

| # | Decision | Choice |
|---|---|---|
| 1 | Folder layout | `Common/`, `ValueObjects/`, `ValueObjects/Ids/`, `Entities/`, `Errors/`, `Policies/` |
| 2 | Value objects | `sealed record` + static `Create`; value-equality free; invalid state unrepresentable |
| 3 | Entities | `sealed class` extending `AuditableEntity`; private setters + domain methods |
| 4 | Domain errors | `DomainException` base + sealed derivatives: `LastSuperadminGuardException`, `SystemRoleProtectedException`, `MenuCycleDetectedException`, `RefreshTokenReuseSignalException`, `InvalidEmailException`, `InvalidPermissionKeyException`, `DeletionPolicyViolationException` |
| 5 | Strongly typed IDs | **Implement in Phase 2.** `readonly record struct` per ID wrapping `Guid` with `New()`, `From(Guid)`, and implicit `Guid` conversion. IDs in `ValueObjects/Ids/`. Domain BCL-only; Infrastructure maps in Phase 3 |
| 6 | Deletion policy | `sealed record DeletionPolicy(SoftDeleteEnabled, RecycleBinVisible, RestoreAllowed, HardDeleteAllowed)`. Each entity exposes `static DeletionPolicy DefaultPolicy`; `Role` overrides per instance when `IsSystem`. Methods check policy and throw |
| 7 | Last-Superadmin guard | `User.RemoveRole(Role, IReadOnlyCollection<User> activeSuperadmins, IClock)` throws if it would leave zero |
| 8 | Superadmin creation gate | `User.AssignRole(Role, IReadOnlyCollection<Role> actorRoles)` throws if role is superadmin and actor lacks it |
| 9 | RefreshToken family revoke | `FamilyId` per token; `IsActive/IsExpired/IsReuseSignal/Rotate/Revoke`. `IsReuseSignal()` = revoked + replaced. Domain returns family id; Application cascades |
| 10 | MenuItem cycle | `MenuItem.SetParent(ParentId?, IReadOnlyCollection<MenuItem> allItems)` walks ancestors, throws if `self` appears |
| 11 | Test framework | xUnit v2.9.3; v3 migration is its own change |
| 12 | Junctions | `UserRole`/`RolePermission` sealed classes with `Assign(assignedBy, clock)` factory + composite identity (e.g. `UserRoleId(UserId, RoleId)`) so the PK is one type, not two parameters that can swap |

### Tradeoff (Decision #5)

| Aspect | Cost | Benefit |
|---|---|---|
| Boilerplate | ~7 ID records + tests before any entity compiles | Compile-time prevention of `Guid` swaps (e.g. `RemoveRole(roleId: x, userId: y)` won't compile) |
| Refactor surface | Touches every entity/property/parameter | Rename-safe; `with`-expression; DDD style |
| EF Core mapping | Deferred Phase 3 (`HasConversion(id => id.Value, value => UserId.From(value))`) | Zero Domain coupling; IDs stay BCL-only |

Cost is one-time at Phase 2 start. `Guid` remains the wire/storage format — the record struct is a typed wrapper, not a different identifier.

## File Changes

| Path | Action |
|------|--------|
| `apps/api/src/Project.Domain/Project.Domain.csproj` | Unchanged (BCL only) |
| `apps/api/src/Project.Domain/Common/{AuditableEntity,IClock,SystemClock}.cs` | Create |
| `apps/api/src/Project.Domain/ValueObjects/Ids/{UserId,RoleId,PermissionId,RefreshTokenId,MenuItemId,UserRoleId,RolePermissionId}.cs` | Create |
| `apps/api/src/Project.Domain/ValueObjects/{Email,PermissionKey}.cs` | Create |
| `apps/api/src/Project.Domain/Policies/DeletionPolicy.cs` | Create |
| `apps/api/src/Project.Domain/Entities/{Permission,Role,RolePermission,User,UserRole,RefreshToken,MenuItem}.cs` | Create |
| `apps/api/src/Project.Domain/Errors/DomainException.cs` + 7 derivatives | Create |
| `apps/api/tests/Project.UnitTests/**/{ValueObjects/Ids,ValueObjects,Entities,Errors,Policies}/*.cs` | Create (ID tests first ~7, then ~30-40 mirroring Domain) |

No deletions. EF Core mapping of typed IDs is **out of scope** and lands in Phase 3 Infrastructure.

## Contracts

```csharp
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(Guid value) => new(value);
    public static implicit operator Guid(UserId id) => id.Value;
}
```

Junction IDs are composites of their two FK IDs (e.g. `UserRoleId(UserId, RoleId)`). `RefreshToken.FamilyId` stays a raw `Guid` (opaque linkage, not a domain ID).

## Testing Strategy

| Layer | What | How |
|---|---|---|
| Unit (Domain) | ID primitives | xUnit `[Fact]`: `New()` unique, `From(Guid)` round-trips, equality value-based, implicit `Guid` conversion identity |
| Unit (Domain) | Spec scenarios + happy/error paths | xUnit `[Fact]`/`[Theory]`. One test class per Domain type |
| Unit (Domain) | Last-Superadmin boundary | `IReadOnlyCollection<User>` of 0/1/2 active Superadmins |
| Unit (Domain) | MenuItem cycle | 3-node tree, attach root under leaf, assert exception |
| Unit (Domain) | RefreshToken reuse | Chain T1→T2→T3, assert `T1.IsReuseSignal()` after T2 rotates |

**TDD order (revised)**: ID records → Email → PermissionKey → DeletionPolicy → Permission → Role → RolePermission → User → UserRole → RefreshToken → MenuItem → exceptions. ID tests are the first commit. EF Core conversion tests land with Phase 3 Infrastructure.

## Migration / Rollout

No migration — Domain has no schema. Phase 4 MigrationService reads these entities via EF Core mappings and seeds the system roles.

## Open Questions

- ~~Strongly typed IDs?~~ → **Resolved:** Phase 2. Junction IDs use a composite-of-FKs record struct — confirm with the team whether they prefer a single synthetic `Guid` per junction row.
- `IClock` in Domain or `Application/Common`? → Domain. Confirm.
- `Email` `+` aliases? → Spec silent; allow.
- `PasswordHash` as typed value object? → Keep `string` (opaque) until Application needs more.
- `MenuItem` keeps both `RequiredPermissionKey` + `RequiredRoleId`? → Yes (spec).
