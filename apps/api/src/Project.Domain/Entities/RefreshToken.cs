using Project.Domain.Common;
using Project.Domain.Errors;
using Project.Domain.Policies;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// Represents a refresh token used in token rotation security.
/// Only SHA-256 hashes are stored (never raw tokens).
/// Tokens rotate on use: the old token is revoked and replaced.
/// Reuse of any revoked token (rotated or admin/logout revoked)
/// triggers family/session revocation.
/// </summary>
public sealed class RefreshToken
{
    public RefreshTokenId Id { get; private set; }
    public string TokenHash { get; private set; }
    public Guid FamilyId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired(IClock clock) => clock.UtcNow >= ExpiresAt;

    public bool IsActive(IClock clock) => !IsRevoked && !IsExpired(clock);

    /// <summary>
    /// Any revoked token, when presented again, signals potential misuse.
    /// This includes tokens revoked via rotation (token theft) and tokens
    /// revoked via explicit logout or administrative action.
    /// The signal indicates the entire family/session must be revoked.
    /// </summary>
    public bool IsReuseSignal => IsRevoked;

    public static DeletionPolicy DefaultPolicy => DeletionPolicy.RefreshTokenDefault;

#pragma warning disable CS8618
    private RefreshToken()
    {
        // Private parameterless constructor for EF Core materialization.
        // Properties are set via their private setters after construction.
    }
#pragma warning restore CS8618

    private RefreshToken(
        RefreshTokenId id,
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt,
        IClock clock)
    {
        Id = id;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAt = expiresAt;
        CreatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="tokenHash">SHA-256 hex string (exactly 64 hex chars, case-insensitive input, normalized to lowercase).</param>
    /// <param name="familyId">Token family identifier. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="expiresAt">Expiration timestamp.</param>
    /// <param name="clock">Clock for the creation timestamp.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenHash"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="tokenHash"/> is empty, whitespace, not exactly 64 characters,
    /// contains non-hexadecimal characters, or when <paramref name="familyId"/> is <see cref="Guid.Empty"/>.
    /// </exception>
    public static RefreshToken Create(
        string tokenHash,
        Guid familyId,
        DateTimeOffset expiresAt,
        IClock clock)
    {
        var normalizedHash = ValidateAndNormalizeTokenHash(tokenHash);

        if (familyId == Guid.Empty)
            throw new ArgumentException("Family ID cannot be empty.", nameof(familyId));

        return new RefreshToken(RefreshTokenId.New(), normalizedHash, familyId, expiresAt, clock);
    }

    /// <summary>
    /// Revokes this token without creating a replacement.
    /// Used for explicit logout or administrative revocation.
    /// </summary>
    public void Revoke(IClock clock)
    {
        RevokedAt = clock.UtcNow;
    }

    /// <summary>
    /// Rotates the token: revokes this one, creates a new token in the same family.
    /// Detects reuse: any already-revoked token (rotated or admin/logout revoked)
    /// throws <see cref="RefreshTokenReuseSignalException"/> to signal that the entire
    /// token family should be revoked.
    /// Rejects expired tokens.
    /// Validates the replacement hash before mutating the current token.
    /// </summary>
    /// <param name="newTokenHash">SHA-256 hex hash of the new refresh token.</param>
    /// <param name="expiresAt">Expiration time for the new token.</param>
    /// <param name="clock">Clock for audit timestamp.</param>
    /// <returns>A new active RefreshToken in the same family.</returns>
    /// <exception cref="RefreshTokenReuseSignalException">
    /// Thrown when this token is already revoked (regardless of how it was revoked),
    /// indicating potential token misuse.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this token is expired.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newTokenHash"/> fails SHA-256 hex validation.
    /// </exception>
    public RefreshToken Rotate(string newTokenHash, DateTimeOffset expiresAt, IClock clock)
    {
        // Finding 3: Any revoked token presented for rotation is a reuse signal
        if (IsRevoked)
            throw new RefreshTokenReuseSignalException(
                $"Refresh token reuse detected for family '{FamilyId}'. " +
                "The entire token family must be revoked as a security measure.");

        // Finding 2: Expired tokens cannot be rotated
        if (IsExpired(clock))
            throw new InvalidOperationException("Cannot rotate an expired token.");

        // Finding 4: Validate replacement BEFORE mutating current token.
        // Create() validates and normalizes the hash; if it throws, 'this' is untouched.
        var newToken = Create(newTokenHash, FamilyId, expiresAt, clock);

        // Now safe to mutate — validation succeeded
        RevokedAt = clock.UtcNow;
        ReplacedByTokenHash = newToken.TokenHash;

        return newToken;
    }

    /// <summary>
    /// Validates that the token hash is a valid SHA-256 hex string and normalizes it to lowercase.
    /// </summary>
    private static string ValidateAndNormalizeTokenHash(string tokenHash)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException(
                "Token hash cannot be empty or whitespace.", nameof(tokenHash));

        if (tokenHash.Length != 64)
            throw new ArgumentException(
                "Token hash must be exactly 64 hexadecimal characters.", nameof(tokenHash));

        for (int i = 0; i < tokenHash.Length; i++)
        {
            char c = tokenHash[i];
            if (!IsHexChar(c))
                throw new ArgumentException(
                    "Token hash must contain only hexadecimal characters (0-9, a-f, A-F).",
                    nameof(tokenHash));
        }

        return tokenHash.ToLowerInvariant();
    }

    private static bool IsHexChar(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}