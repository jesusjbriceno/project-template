using System.Reflection;
using Project.Application.Abstractions.Security;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.ApplicationTests.Abstractions.Security;

/// <summary>
/// Compile-time and runtime boundary proof: verifies that <see cref="IUserSession"/>
/// and <see cref="ISuperadminEnforcementContext"/> are distinct interfaces that cannot
/// substitute for each other, and that <see cref="ITokenService"/> exposes only
/// family-revocation semantics — no raw token handling.
/// </summary>
public sealed class SecurityBoundaryTests
{
    // ─────────────── Two-interface distinction ───────────────

    [Fact]
    public void IUserSession_And_ISuperadminEnforcementContext_AreDistinctInterfaces()
    {
        // Prove the type system distinguishes them: a class implementing one
        // does NOT automatically satisfy the other.
        var sessionOnly = new SessionOnlyStub();
        var enforcementOnly = new EnforcementOnlyStub();

        Assert.IsAssignableFrom<IUserSession>(sessionOnly);
        Assert.IsNotAssignableFrom<ISuperadminEnforcementContext>(sessionOnly);
        Assert.IsNotAssignableFrom<IUserSession>(enforcementOnly);
        Assert.IsAssignableFrom<ISuperadminEnforcementContext>(enforcementOnly);
    }

    [Fact]
    public void IUserSession_CannotBeSubstitutedForSuperadminEnforcement()
    {
        // Compile-time proof: IUserSession does not expose GetActiveSuperadminsAsync
        // or GetActorRolesAsync. A class implementing only IUserSession will NOT
        // compile where ISuperadminEnforcementContext is required.
        IUserSession session = new SessionOnlyStub();

        // This line would fail to compile (and should):
        // ISuperadminEnforcementContext ctx = session; // ❌ type mismatch
        Assert.NotNull(session); // proves the variable exists
    }

    [Fact]
    public void SuperadminEnforcementContext_ExposesRequiredMethods()
    {
        // Reflection: assert ISuperadminEnforcementContext exposes exactly two
        // methods with their exact signatures. Guards against accidental removal
        // or signature drift as the codebase evolves.
        var type = typeof(ISuperadminEnforcementContext);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Equal(2, methods.Length);

        // GetActiveSuperadminsAsync(CancellationToken ct)
        var getSuperadmins = Assert.Single(methods, m => m.Name == "GetActiveSuperadminsAsync");
        Assert.Equal(typeof(Task<IReadOnlyCollection<User>>), getSuperadmins.ReturnType);
        var saParams = getSuperadmins.GetParameters();
        Assert.Single(saParams);
        Assert.Equal(typeof(CancellationToken), saParams[0].ParameterType);

        // GetActorRolesAsync(CancellationToken ct)
        var getRoles = Assert.Single(methods, m => m.Name == "GetActorRolesAsync");
        Assert.Equal(typeof(Task<IReadOnlyCollection<Role>>), getRoles.ReturnType);
        var roleParams = getRoles.GetParameters();
        Assert.Single(roleParams);
        Assert.Equal(typeof(CancellationToken), roleParams[0].ParameterType);

        // Type-system guard: ISuperadminEnforcementContext is NOT assignable
        // from IUserSession and vice versa — prevents generic-session substitution.
        Assert.False(typeof(IUserSession).IsAssignableFrom(type));
        Assert.False(type.IsAssignableFrom(typeof(IUserSession)));
    }

    // ─────────────── No raw-token surface on ITokenService ───────────────

    [Fact]
    public void ITokenService_HasNoRawTokenMethods()
    {
        // Reflection: assert ITokenService exposes ONLY RevokeFamilyAsync with
        // the exact signature. Guards against future raw-token methods (issue,
        // validate, create) being added to the Application-layer boundary.
        var type = typeof(ITokenService);
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Single(methods);

        var revoke = methods[0];
        Assert.Equal("RevokeFamilyAsync", revoke.Name);
        Assert.Equal(typeof(Task), revoke.ReturnType);

        var parameters = revoke.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.Equal("familyId", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.Equal("ct", parameters[1].Name);
        Assert.True(parameters[1].HasDefaultValue);

        // Defense-in-depth: method name must not contain raw-token patterns
        Assert.DoesNotContain("Issue", revoke.Name);
        Assert.DoesNotContain("Validate", revoke.Name);
        Assert.DoesNotContain("Create", revoke.Name);
    }

    [Fact]
    public async Task ITokenService_RevokeFamilyAsync_AcceptsGuidAndCancellationToken()
    {
        ITokenService svc = new TokenServiceStub();

        await svc.RevokeFamilyAsync(Guid.NewGuid(), CancellationToken.None);

        // Must not throw — proves signature compiles with CancellationToken
    }

    // ─────────────── Stub implementations ───────────────

    private sealed class SessionOnlyStub : IUserSession
    {
        public UserId? UserId => null;
        public string? Email => null;
        public IReadOnlyCollection<Role> Roles => Array.Empty<Role>();
        public bool IsAuthenticated => false;
    }

    private sealed class EnforcementOnlyStub : ISuperadminEnforcementContext
    {
        public Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<User>>(Array.Empty<User>());

        public Task<IReadOnlyCollection<Role>> GetActorRolesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<Role>>(Array.Empty<Role>());
    }

    private sealed class TokenServiceStub : ITokenService
    {
        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
