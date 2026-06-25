using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;

namespace Project.Infrastructure.Security;

/// <summary>
/// Thin adapter implementing <see cref="ITokenService"/> over <see cref="IRefreshTokenRepository"/>.
/// Preserves the Application security boundary — no raw token handling, no key material,
/// no JWT internals. Only family-level revocation is exposed here.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public TokenService(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository
            ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
    }

    /// <inheritdoc />
    public async Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
    {
        await _refreshTokenRepository.RevokeFamilyAsync(familyId, ct);
    }
}
