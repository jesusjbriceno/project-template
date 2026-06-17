using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRoleRepository"/>.
/// Extends <see cref="BaseRepository{TEntity,TId}"/> with role-specific queries:
/// existence check and system-role pre-loading.
/// </summary>
public sealed class RoleRepository : BaseRepository<Role, RoleId>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(RoleId id, CancellationToken ct = default)
    {
        return await Set.AnyAsync(r => r.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Role>> GetSystemRolesAsync(CancellationToken ct = default)
    {
        return await Set
            .Where(r => r.IsSystem)
            .ToListAsync(ct);
    }
}
