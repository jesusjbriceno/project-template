using Project.Domain.Entities;

namespace Project.Application.Abstractions.Security;

/// <summary>
/// Controlled context for Superadmin invariant enforcement.
/// Pre-loads the collections required by Domain methods that guard
/// superadmin lifecycle operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface MUST NOT be substituted by a generic <see cref="IUserSession"/>.
/// The type system enforces this: handlers that need superadmin enforcement
/// must explicitly depend on <see cref="ISuperadminEnforcementContext"/>,
/// not <see cref="IUserSession"/>.
/// </para>
/// <para>
/// Use cases MUST pre-load <c>activeSuperadmins</c> and <c>actorRoles</c>
/// via this context before calling Domain methods that require them.
/// </para>
/// </remarks>
public interface ISuperadminEnforcementContext
{
    /// <summary>
    /// Returns all currently active users who hold the superadmin role.
    /// Required by <see cref="User.Deactivate"/>, <see cref="User.RemoveRole"/>,
    /// and <see cref="User.Delete"/> to enforce the last-active-superadmin invariant.
    /// </summary>
    Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct);

    /// <summary>
    /// Returns the roles held by the actor performing the current operation.
    /// Required by <see cref="User.AssignRole"/> to enforce the
    /// "only superadmins can assign superadmin role" invariant.
    /// </summary>
    Task<IReadOnlyCollection<Role>> GetActorRolesAsync(CancellationToken ct);
}
