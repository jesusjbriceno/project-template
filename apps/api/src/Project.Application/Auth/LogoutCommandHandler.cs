using System.Security.Cryptography;
using System.Text;
using Project.Application.Abstractions.Messaging;
using Project.Domain.Common;
using Project.Application.Common;

namespace Project.Application.Auth;

/// <summary>
/// Handles logout: looks up the refresh token by hash, revokes it,
/// and triggers family-wide revocation via <see cref="IRefreshTokenRepository.RevokeFamilyAsync"/>.
/// Idempotent — already revoked or missing tokens return success.
/// </summary>
public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IClock _clock;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IClock clock)
    {
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Revokes the token family for the presented refresh token.
    /// Returns success even when the token is not found (idempotent logout).
    /// </summary>
    public async Task<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tokenHash = ComputeSha256Hash(command.RefreshTokenRaw);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (existingToken is null)
            return Result.Success();

        existingToken.Revoke(_clock);
        await _refreshTokenRepository.RevokeFamilyAsync(existingToken.FamilyId, ct);

        return Result.Success();
    }

    private static string ComputeSha256Hash(string raw)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hashBytes);
    }
}
