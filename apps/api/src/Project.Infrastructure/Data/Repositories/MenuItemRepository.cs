using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IMenuItemRepository"/>.
/// Extends <see cref="BaseRepository{TEntity,TId}"/> with menu-specific queries:
/// full hierarchy loading and direct children lookup.
/// Soft-deleted items are excluded by the query filter configured in
/// <see cref="Configurations.MenuItemConfiguration"/>.
/// </summary>
public sealed class MenuItemRepository : BaseRepository<MenuItem, MenuItemId>, IMenuItemRepository
{
    public MenuItemRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<MenuItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await Set.ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<MenuItem>> GetChildrenAsync(
        MenuItemId parentId, CancellationToken ct = default)
    {
        return await Set
            .Where(m => m.ParentId != null && m.ParentId.Equals(parentId))
            .ToListAsync(ct);
    }
}
