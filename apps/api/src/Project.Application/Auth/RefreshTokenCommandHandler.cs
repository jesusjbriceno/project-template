using System.Security.Cryptography;
using System.Text;
using Project.Application.Abstractions.Messaging;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.Errors;
using Project.Domain.ValueObjects.Ids;
using Project.Application.Common;

namespace Project.Application.Auth;

/// <summary>
/// Handles refresh token rotation: validates the presented token,
/// rotates it (issuing a new token in the same family), and generates
/// a fresh access token. Catches reuse signals and triggers family-wide
/// revocation as a security measure.
/// </summary>
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, TokenPairDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IClock clock,
        ITokenService tokenService,
        IUserRepository userRepository)
    {
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Validates and rotates the refresh token, issuing a new access token.
    /// User lookup happens BEFORE any mutation to prevent persisting tokens
    /// for blocked or deleted users.
    /// </summary>
    public async Task<Result<TokenPairDto>> Handle(RefreshTokenCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        // ── Compute SHA-256 hash of the raw token ──
        var tokenHash = ComputeSha256Hash(command.RefreshTokenRaw);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (existingToken is null)
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.TokenExpired,
                "Refresh token not found or expired.");

        // ── Expired check ──
        if (existingToken.IsExpired(_clock))
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.TokenExpired,
                "Refresh token has expired.");

        // ── Reuse detection ──
        if (existingToken.IsReuseSignal)
        {
            await _tokenService.RevokeFamilyAsync(existingToken.FamilyId, ct);
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.TokenReuseDetected,
                "Token reuse detected. Token family has been revoked.");
        }

        // ── Look up user BEFORE any mutation (F5: no persistence for blocked/missing users) ──
        var (user, roles) = await _userRepository.GetByIdWithRolesAsync(existingToken.UserId, ct);
        if (user is null || user.IsBlocked)
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.InvalidCredentials,
                "Invalid credentials.");

        // ── Rotate: generate new token and replace current ──
        var (newRaw, newHash) = _jwtTokenService.GenerateRefreshToken();
        var newExpiry = _clock.UtcNow.AddDays(7);

        RefreshToken newToken;
        try
        {
            newToken = existingToken.Rotate(newHash, newExpiry, _clock);
        }
        catch (RefreshTokenReuseSignalException)
        {
            await _tokenService.RevokeFamilyAsync(existingToken.FamilyId, ct);
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.TokenReuseDetected,
                "Token reuse detected. Token family has been revoked.");
        }
        catch (InvalidOperationException)
        {
            return Result<TokenPairDto>.Failure(
                ErrorCodes.Auth.TokenExpired,
                "Refresh token has expired.");
        }

        // ── Persist: old token revocation + new token creation ──
        // F1: Tell the repository the old token was modified (RevokedAt, ReplacedByTokenHash)
        // so NoTracking EF persists the changes together with the new token.
        _refreshTokenRepository.Update(existingToken);
        await _refreshTokenRepository.AddAsync(newToken, ct);

        // ── Issue new access token ──
        var (accessToken, lifetime) = _jwtTokenService.GenerateAccessToken(user, roles);

        return Result<TokenPairDto>.Success(new TokenPairDto(
            accessToken,
            (int)lifetime.TotalSeconds,
            newRaw));
    }

    private static string ComputeSha256Hash(string raw)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hashBytes);
    }
}
