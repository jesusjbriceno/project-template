using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing repository contract for <see cref="MenuItem"/> aggregate persistence.
/// Inherits common CRUD and paginated search from <see cref="IBaseRepository{TEntity,TId}"/>.
/// Adds menu-specific queries: hierarchical tree loading, children lookup.
/// Does NOT reference EF Core, DbContext, IQueryable, or Infrastructure types.
/// </summary>
public interface IMenuItemRepository : IBaseRepository<MenuItem, MenuItemId>
{
    /// <summary>
    /// Returns the complete menu hierarchy. Used by <see cref="MenuItem.SetParent"/>
    /// for cycle detection and by authorization-driven menu rendering.
    /// </summary>
    Task<IReadOnlyCollection<MenuItem>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns direct children of the given parent menu item.
    /// </summary>
    Task<IReadOnlyCollection<MenuItem>> GetChildrenAsync(
        MenuItemId parentId, CancellationToken ct = default);
}
