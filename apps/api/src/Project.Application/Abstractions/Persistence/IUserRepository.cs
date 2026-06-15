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
    /// Checks whether a user with the given identifier exists.
    /// </summary>
    Task<bool> ExistsAsync(UserId id, CancellationToken ct = default);

    /// <summary>
    /// Pre-loads all currently active superadmin users for Domain invariant enforcement.
    /// Required by <see cref="ISuperadminEnforcementContext"/> and superadmin guards.
    /// </summary>
    Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct = default);
}
