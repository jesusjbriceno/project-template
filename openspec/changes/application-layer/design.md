# Design: Application Layer Foundation

## Technical Approach

Foundation slice defines the Application-layer contracts the whole backend obeys: `Result`/`Result<T>`, CQRS command/query handler signatures, per-aggregate repository interfaces, a controlled `ISuperadminEnforcementContext`, and a generic `IUserSession`/`ITokenService` security boundary. All code is interface- and type-only — no Infrastructure, EF Core, ASP.NET, or JWT concerns leak in. Small, TDD-driven, fits the 400-line review budget so architectural intent is reviewable before use-case slices pile on.

## Architecture Decisions

| # | Decision | Choice | Rejected | Rationale |
|---|---|---|---|---|
| 1 | Result pattern | Custom `Result` + `Result<T>` + `Error` | `FluentResults` / `Ardalis.Result` | Zero deps; ~80 LOC is enough to own. |
| 2 | CQRS shape | Custom `ICommand`/`IQuery`/`IHandler` only | MediatR now | Explicit DI, no reflection; spec allows deferral. |
| 3 | Repo granularity | Per-aggregate interfaces | Generic `IRepository<T>` / per-method | Matches aggregates; preserves `GetActiveSuperadminsAsync` semantics. |
| 4 | Security split | `IUserSession` separate from `ISuperadminEnforcementContext` | One session with superadmin methods | Boundary enforced by type system; spec forbids generic-session substitution. |
| 5 | `ITokenService` | `RevokeFamilyAsync` only | Full token service | App never handles raw tokens; Infra implements with hasher. |
| 6 | Validation | `IValidated` marker only | Full FluentValidation pipeline now | Pipeline behavior belongs with first use-case slice. |

## Data Flow

```
Controller → ICommand / IQuery
                ↓
   ICommandHandler<T> / IQueryHandler<T,TResponse>
                ↓ uses
   IUserRepository · ISuperadminEnforcementContext · IUserSession
                ↓
        Result / Result<T> back to caller
```

Handlers MUST: (1) pre-load `activeSuperadmins`/`actorRoles` via `ISuperadminEnforcementContext` before Domain methods that require them; (2) catch `RefreshTokenReuseSignalException` → `ITokenService.RevokeFamilyAsync`; (3) map Domain exceptions → `Result.Failure` with stable codes.

## File Changes

| File | Action |
|------|--------|
| `apps/api/src/Project.Application/Common/{Result,ResultT,Error}.cs` | Create — `Result`, `Result<T>`, `Error` + stable codes. |
| `apps/api/src/Project.Application/Abstractions/Messaging/ICommand{,Handler}.cs` + `IQuery{,Handler}.cs` | Create — CQRS contract pair. |
| `apps/api/src/Project.Application/Abstractions/Persistence/I{User,Role,Permission,RefreshToken,MenuItem}Repository.cs` | Create — per-aggregate repos (5 files). |
| `apps/api/src/Project.Application/Abstractions/Security/I{UserSession,SuperadminEnforcementContext,TokenService}.cs` | Create — security boundary. |
| `apps/api/src/Project.Application/Abstractions/Validation/IValidated.cs` | Create — pipeline marker. |
| `apps/api/src/Project.Application/GlobalUsings.cs` | Create — shared usings. |
| `apps/api/tests/Project.ApplicationTests/Common/{Result,Error}Tests.cs` | Create — TDD for `Result`/`Error`. |
| `apps/api/tests/Project.ApplicationTests/Abstractions/Messaging/CqrsContractTests.cs` | Create — compile-time signature proof. |
| `apps/api/tests/Project.ApplicationTests/Abstractions/Persistence/RepositoryContractTests.cs` | Create — hand-rolled stubs prove types. |
| `apps/api/tests/Project.ApplicationTests/Abstractions/Security/SecurityBoundaryTests.cs` | Create — two-interface distinction; no raw-token surface. |

**Total: 16 new files (10 src + 6 tests).** No modifications, no deletions.

## Interfaces / Contracts

```csharp
public readonly record struct Error(string Code, string Message);
public sealed class Result { bool IsSuccess { get; } Error Error { get; } }
public sealed class Result<T> {
    bool IsSuccess { get; } T? Value { get; } Error Error { get; }
    public static implicit operator Result<T>(T value);
    public static implicit operator Result<T>(Error error);
}
public interface ICommand { }
public interface IQuery<TResponse> { }
public interface ICommandHandler<in TCommand> where TCommand : ICommand
    { Task<Result> Handle(TCommand c, CancellationToken ct); }
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
    { Task<Result<TResponse>> Handle(TQuery q, CancellationToken ct); }
public interface ISuperadminEnforcementContext {
    Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct);
    Task<IReadOnlyCollection<Role>> GetActorRolesAsync(CancellationToken ct);
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit (xUnit v2) | `Result`/`Result<T>` factories, `IsSuccess`/`IsFailure`, implicit conv. | `[Fact]` per behavior; `[Theory]` over stable-code set. |
| Unit | CQRS compile-time contracts | Sample command/query in a test class enforces `CancellationToken` + return type. |
| Unit | Repository interfaces | Hand-rolled stubs (no Moq dep) prove return types and async signatures. |
| Unit | Security boundary | Type-shape assertion: `IUserSession` ≠ `ISuperadminEnforcementContext`; `ITokenService` has no raw-token methods. |
| Integration | None this slice | Deferred — needs Infrastructure. |

TDD order: `ResultTests` → `Result`; `ErrorTests` → `Error`; CQRS contract → interfaces; repo contract → interfaces; security boundary → interfaces.

## Migration / Rollout

No migration. Application project is empty (no `.cs` source files). Verify with `dotnet build apps/api/src/Project.Application` — must succeed with zero EF Core, ASP.NET, or Infrastructure references.

## Out of Scope (Explicit)

Concrete handlers; EF Core / DbContext / migrations; JWT issuance/validation; login/logout; API endpoints; ProblemDetails mapping; OpenAPI; MediatR; FluentValidation pipeline behavior (only the `IValidated` marker ships).

## Open Questions

- [ ] `Result<T>.Value` as `T?` vs `T` — foundation ships `T?`.
- [ ] Per-domain error codes: foundation defines the common set; per-domain codes land with their use-case slice.
- [ ] `ISuperadminEnforcementContext` scope: foundation leaves it lifetime-agnostic; Infrastructure/API wire it.
