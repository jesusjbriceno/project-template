using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Application.Abstractions.Security;

/// <summary>
/// Represents the current authenticated user session.
/// Exposes identity and authorization context to Application-layer handlers
/// without leaking HTTP context, JWT claims, or Infrastructure concerns.
/// </summary>
/// <remarks>
/// This interface is intentionally generic. It MUST NOT expose:
/// raw tokens, HTTP context, JWT claims, or token issuance/validation.
/// Handlers obtain identity via this interface, never from HTTP context.
/// </remarks>
public interface IUserSession
{
    /// <summary>
    /// The current user's identifier, or null when unauthenticated.
    /// </summary>
    UserId? UserId { get; }

    /// <summary>
    /// The current user's email, or null when unauthenticated.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Roles held by the current user. Empty when unauthenticated.
    /// </summary>
    IReadOnlyCollection<Role> Roles { get; }

    /// <summary>
    /// True when the session represents an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }
}
