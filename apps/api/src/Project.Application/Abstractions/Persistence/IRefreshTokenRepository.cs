using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing repository contract for <see cref="RefreshToken"/> persistence.
/// Inherits common CRUD and paginated search from <see cref="IBaseRepository{TEntity,TId}"/>.
/// Adds token-specific queries: hash-based lookup, family revocation, active-family loading.
/// Does NOT reference EF Core, DbContext, IQueryable, or Infrastructure types.
/// </summary>
public interface IRefreshTokenRepository : IBaseRepository<RefreshToken, RefreshTokenId>
{
    /// <summary>
    /// Retrieves a refresh token by its SHA-256 hash, or null if not found.
    /// The hash is a normalized lowercase hex string — never the raw token.
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Revokes every active token in the given family.
    /// Used when <see cref="Domain.Errors.RefreshTokenReuseSignalException"/> is caught.
    /// </summary>
    Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-revoked, non-expired tokens for the given family.
    /// Used during rotation to detect anomalies.
    /// </summary>
    Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(
        Guid familyId, CancellationToken ct = default);
}
