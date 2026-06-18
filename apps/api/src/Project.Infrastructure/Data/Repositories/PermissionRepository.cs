using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPermissionRepository"/>.
/// Extends <see cref="BaseRepository{TEntity,TId}"/> with permission-specific queries:
/// key-based lookup and key-existence check.
/// </summary>
public sealed class PermissionRepository : BaseRepository<Permission, PermissionId>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<Permission?> GetByKeyAsync(PermissionKey key, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(p => p.Key == key, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByKeyAsync(PermissionKey key, CancellationToken ct = default)
    {
        return await Set.AnyAsync(p => p.Key == key, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct = default)
    {
        return await Set.AsNoTracking().ToListAsync(ct);
    }
}
