using System.Security.Claims;
using Project.Domain.Entities;

namespace Project.Application.Abstractions.Security;

/// <summary>
/// Application-layer contract for JWT token issuance and validation.
/// Decouples Domain/Application code from raw JWT key material and crypto details.
/// Implementations own HS256 signing, expiry, and claim construction —
/// Application code only sees the typed interface.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT access token with standard claims:
    /// sub, email, roles[], jti, iss, aud, exp, iat.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="roles">The user's role collection for the roles[] claim.</param>
    /// <returns>A tuple containing the signed token string and its lifetime.</returns>
    (string Token, TimeSpan Lifetime) GenerateAccessToken(User user, IReadOnlyCollection<Role> roles);

    /// <summary>
    /// Generates a cryptographically random refresh token (raw bytes)
    /// and its SHA-256 hash (64-char lowercase hex, ready for storage).
    /// </summary>
    /// <returns>A tuple containing the raw token and its SHA-256 hash.</returns>
    (string Raw, string Hash) GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token and returns the claims principal.
    /// Returns null when the token is invalid, tampered, or expired.
    /// </summary>
    /// <param name="token">The raw JWT string to validate.</param>
    /// <returns>A ClaimsPrincipal when valid; null otherwise.</returns>
    ClaimsPrincipal? ValidateAccessToken(string token);
}
