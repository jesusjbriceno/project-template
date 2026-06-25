using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing repository contract for <see cref="User"/> aggregate persistence.
/// Inherits common CRUD and paginated search from <see cref="IBaseRepository{TEntity,TId}"/>.
/// Adds user-specific queries: email lookup, superadmin pre-loading, existence check.
/// Does NOT reference EF Core, DbContext, IQueryable, or Infrastructure types.
/// </summary>
public interface IUserRepository : IBaseRepository<User, UserId>
{
    /// <summary>
    /// Retrieves a user by email address, or null if not found.
    /// </summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by email with their current roles.
    /// Since <see cref="UserRole"/> has no navigation to <see cref="Role"/>,
    /// the Infrastructure layer uses an explicit join/include to populate both.
    /// Returns null user with empty roles when the user is not found.
    /// </summary>
    Task<(User? User, IReadOnlyCollection<Role> Roles)> GetByEmailWithRolesAsync(
        Email email, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by ID with their current roles.
    /// Mirrors <see cref="GetByEmailWithRolesAsync"/> but uses <see cref="UserId"/>.
    /// </summary>
    Task<(User? User, IReadOnlyCollection<Role> Roles)> GetByIdWithRolesAsync(
        UserId id, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user with the given identifier exists.
    /// </summary>
    Task<bool> ExistsAsync(UserId id, CancellationToken ct = default);

    /// <summary>
    /// Pre-loads all currently active superadmin users for Domain invariant enforcement.
    /// Required by <see cref="ISuperadminEnforcementContext"/> and superadmin guards.
    /// </summary>
    Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct = default);
}
