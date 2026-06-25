using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Project.Application.Abstractions.Security;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Security;

namespace Project.UnitTests.Security;

public class UserSessionTests
{
    [Fact]
    public void AuthenticatedUser_ReturnsCorrectUserId()
    {
        // ARRANGE
        var userId = UserId.New();
        var principal = CreatePrincipal(
            sub: userId.ToString(),
            email: "test@example.com",
            roles: new[] { "admin" });
        var session = CreateSession(principal);

        // ACT & ASSERT
        Assert.True(session.IsAuthenticated);
        Assert.NotNull(session.UserId);
        Assert.Equal(userId, session.UserId);
    }

    [Fact]
    public void AuthenticatedUser_ReturnsCorrectEmail()
    {
        // ARRANGE
        var principal = CreatePrincipal(
            sub: UserId.New().ToString(),
            email: "test@example.com",
            roles: new[] { "editor" });
        var session = CreateSession(principal);

        // ACT & ASSERT
        Assert.Equal("test@example.com", session.Email);
    }

    [Fact]
    public void AuthenticatedUser_ReturnsCorrectRoles()
    {
        // ARRANGE
        var principal = CreatePrincipal(
            sub: UserId.New().ToString(),
            email: "test@example.com",
            roles: new[] { "admin", "editor", "viewer" });
        var session = CreateSession(principal);

        // ACT
        var roles = session.Roles;

        // ASSERT
        Assert.NotNull(roles);
        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.Name == "admin");
        Assert.Contains(roles, r => r.Name == "editor");
        Assert.Contains(roles, r => r.Name == "viewer");
    }

    [Fact]
    public void Unauthenticated_NoUserPrincipal_ReturnsNullAndEmpty()
    {
        // ARRANGE — HttpContext with no user
        var session = CreateSession(principal: null);

        // ACT & ASSERT
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.Null(session.Email);
        Assert.Empty(session.Roles);
    }

    [Fact]
    public void PrincipalMissingSubClaim_ReturnsNotAuthenticated()
    {
        // ARRANGE — principal without sub claim
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Email, "test@example.com"));
        var principal = new ClaimsPrincipal(identity);
        var session = CreateSession(principal);

        // ACT & ASSERT
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
    }

    [Fact]
    public void PrincipalWithInvalidSubGuid_ReturnsNotAuthenticated()
    {
        // ARRANGE — sub claim with non-GUID value
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, "test@example.com"));
        var principal = new ClaimsPrincipal(identity);
        var session = CreateSession(principal);

        // ACT & ASSERT
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
    }

    [Fact]
    public void NoRolesClaim_ReturnsEmptyRoles()
    {
        // ARRANGE
        var principal = CreatePrincipal(
            sub: UserId.New().ToString(),
            email: "test@example.com",
            roles: Array.Empty<string>());
        var session = CreateSession(principal);

        // ACT
        var roles = session.Roles;

        // ASSERT
        Assert.NotNull(roles);
        Assert.Empty(roles);
    }

    [Fact]
    public void PrincipalWithValidSub_ButIdentityNotAuthenticated_IsNotAuthenticated()
    {
        // ARRANGE — ClaimsIdentity without auth type is NOT authenticated
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, UserId.New().ToString()));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, "test@example.com"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
        var principal = new ClaimsPrincipal(identity);
        var session = CreateSession(principal);

        // ACT & ASSERT
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public void UnauthenticatedPrincipal_DoesNotExposeEmailOrRoles()
    {
        // ARRANGE — ClaimsIdentity without auth type: not authenticated but has claims
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, UserId.New().ToString()));
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, "leaked@example.com"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
        var principal = new ClaimsPrincipal(identity);
        var session = CreateSession(principal);

        // ACT & ASSERT — identity data must not be exposed when not authenticated
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.Null(session.Email);
        Assert.Empty(session.Roles);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal with standard JWT claims.
    /// </summary>
    private static ClaimsPrincipal CreatePrincipal(string sub, string email, string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, sub),
            new(JwtRegisteredClaimNames.Email, email),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates a UserSession backed by a fake HttpContext with the given principal.
    /// </summary>
    private static IUserSession CreateSession(ClaimsPrincipal? principal)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = principal ?? new ClaimsPrincipal();
        var accessor = new FakeHttpContextAccessor(httpContext);
        return new UserSession(accessor);
    }

    /// <summary>
    /// Fake IHttpContextAccessor for unit testing UserSession.
    /// </summary>
    private sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public FakeHttpContextAccessor(HttpContext httpContext)
        {
            HttpContext = httpContext;
        }

        public HttpContext? HttpContext { get; set; }
    }
}
