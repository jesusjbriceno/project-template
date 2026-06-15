using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing repository contract for <see cref="Permission"/> aggregate persistence.
/// Inherits common CRUD and paginated search from <see cref="IBaseRepository{TEntity,TId}"/>.
/// Adds permission-specific queries: key-based lookup, key-existence check.
/// Does NOT reference EF Core, DbContext, IQueryable, or Infrastructure types.
/// </summary>
public interface IPermissionRepository : IBaseRepository<Permission, PermissionId>
{
    /// <summary>
    /// Retrieves a permission by its unique <see cref="PermissionKey"/>, or null if not found.
    /// </summary>
    Task<Permission?> GetByKeyAsync(PermissionKey key, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a permission with the given key already exists.
    /// </summary>
    Task<bool> ExistsByKeyAsync(PermissionKey key, CancellationToken ct = default);
}
