using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRefreshTokenRepository"/>.
/// Extends <see cref="BaseRepository{TEntity,TId}"/> with token-specific queries:
/// hash-based lookup, active-family loading, and family-wide revocation.
/// </summary>
public sealed class RefreshTokenRepository : BaseRepository<RefreshToken, RefreshTokenId>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        return await Set.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);
    }
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(
        Guid familyId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await Set
            .Where(rt => rt.FamilyId == familyId)
            .Where(rt => rt.RevokedAt == null)
            .Where(rt => rt.ExpiresAt > now)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
    {
        await Set
            .Where(rt => rt.FamilyId == familyId)
            .Where(rt => rt.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedAt, DateTimeOffset.UtcNow),
                ct);
    }
}
