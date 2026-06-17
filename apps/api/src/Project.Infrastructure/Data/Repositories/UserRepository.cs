using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// Extends <see cref="BaseRepository{TEntity,TId}"/> with user-specific queries:
/// email lookup, existence check, and active-superadmin pre-loading.
/// </summary>
public sealed class UserRepository : BaseRepository<User, UserId>, IUserRepository
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
    {
        // Compare Email directly — EF Core translates via EmailConverter
        return await Set
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(UserId id, CancellationToken ct = default)
    {
        return await Set.AnyAsync(u => u.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct = default)
    {
        // Find superadmin role IDs first (system role named "superadmin")
        var superadminRoleIds = await DbContext.Set<Role>()
            .Where(r => r.IsSystem && r.Name.ToLower() == "superadmin")
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (superadminRoleIds.Count == 0)
            return Array.Empty<User>();

        return await Set
            .Where(u => u.IsActive)
            .Where(u => u.UserRoles.Any(ur => superadminRoleIds.Contains(ur.RoleId)))
            .ToListAsync(ct);
    }
}
