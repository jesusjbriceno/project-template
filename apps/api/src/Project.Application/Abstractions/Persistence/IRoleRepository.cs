using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing repository contract for <see cref="Role"/> aggregate persistence.
/// Inherits common CRUD and paginated search from <see cref="IBaseRepository{TEntity,TId}"/>.
/// Adds role-specific queries: system-role pre-loading, existence check.
/// Does NOT reference EF Core, DbContext, IQueryable, or Infrastructure types.
/// </summary>
public interface IRoleRepository : IBaseRepository<Role, RoleId>
{
    /// <summary>
    /// Checks whether a role with the given identifier exists.
    /// </summary>
    Task<bool> ExistsAsync(RoleId id, CancellationToken ct = default);

    /// <summary>
    /// Returns all system-defined roles. Used during authorization and superadmin checks.
    /// </summary>
    Task<IReadOnlyCollection<Role>> GetSystemRolesAsync(CancellationToken ct = default);
}
