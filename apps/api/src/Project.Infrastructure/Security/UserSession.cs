using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Security;

/// <summary>
/// Real <see cref="IUserSession"/> implementation backed by <see cref="IHttpContextAccessor"/>.
/// Extracts identity and authorization context from the current HTTP request's
/// authenticated <see cref="ClaimsPrincipal"/>. Replaces the scaffold <c>NullUserSession</c>.
/// </summary>
/// <remarks>
/// This class resolves identity from JWT claims (sub, email, role).
/// It MUST NOT touch HTTP context directly beyond the ClaimsPrincipal provided
/// by <see cref="IHttpContextAccessor"/>.
/// </remarks>
public sealed class UserSession : IUserSession
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserSession(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public UserId? UserId
    {
        get
        {
            if (!IsAuthenticated) return null;

            var sub = FindClaimValue(JwtRegisteredClaimNames.Sub);
            if (sub is null) return null;

            if (Guid.TryParse(sub, out var guid))
                return UserId.From(guid);

            return null;
        }
    }

    /// <inheritdoc />
    public string? Email => IsAuthenticated ? FindClaimValue(JwtRegisteredClaimNames.Email) : null;

    /// <inheritdoc />
    public IReadOnlyCollection<Role> Roles
    {
        get
        {
            if (!IsAuthenticated)
                return Array.Empty<Role>();

            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal is null)
                return Array.Empty<Role>();

            return principal
                .FindAll(ClaimTypes.Role)
                .Select(c => CreateRoleFromName(c.Value))
                .ToList()
                .AsReadOnly();
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity is null)
                return false;

            if (!principal.Identity.IsAuthenticated)
                return false;

            // Also require a parseable 'sub' claim — both conditions must hold.
            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (sub is null || !Guid.TryParse(sub, out _))
                return false;

            return true;
        }
    }

    /// <summary>
    /// Retrieves the value of a claim from the current user principal.
    /// </summary>
    private string? FindClaimValue(string claimType)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal is null) return null;

        return principal.FindFirstValue(claimType);
    }

    /// <summary>
    /// Creates a lightweight <see cref="Role"/> from a claim string.
    /// The role name "superadmin" (case-insensitive) is marked as a system role
    /// so that superadmin guard checks in the Domain layer work correctly.
    /// These roles are session-scoped and never persisted.
    /// </summary>
    private static Role CreateRoleFromName(string name)
    {
        bool isSystem = "superadmin".Equals(name, StringComparison.OrdinalIgnoreCase);
        return Role.Create(name, isSystem, "system", new SystemClock());
    }
}
