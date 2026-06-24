using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Domain.Entities;

namespace Project.Infrastructure.Security;

/// <summary>
/// HS256 JWT token service. Owns the JWT signing key and token validation logic.
/// Implements <see cref="IJwtTokenService"/> so Application code never touches
/// key material or JWT internals.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtTokenService(JwtOptions options, IClock clock)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        _tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
    }

    /// <inheritdoc />
    public (string Token, TimeSpan Lifetime) GenerateAccessToken(User user, IReadOnlyCollection<Role> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, _clock.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        var jwtToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: _clock.UtcNow.Add(AccessTokenLifetime).UtcDateTime,
            signingCredentials: _signingCredentials);

        return (_tokenHandler.WriteToken(jwtToken), AccessTokenLifetime);
    }

    /// <inheritdoc />
    public (string Raw, string Hash) GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);

        var raw = Base64UrlEncode(randomBytes);

        // Hash the raw (base64url) string so that the stored hash can be
        // recomputed directly from the token the client presents — no decode step.
        var hash = ComputeSha256Hash(raw);

        return (raw, hash);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = false, // replaced by custom LifetimeValidator using _clock
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (DateTime? notBefore, DateTime? expires, SecurityToken securityToken,
                TokenValidationParameters _) =>
            {
                // Reject if the token has no expiration
                if (expires is null) return false;

                // Reject if the expiration is in the past according to our injected clock
                var now = _clock.UtcNow.UtcDateTime;
                return expires > now;
            },
        };

        try
        {
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Encodes bytes to a Base64URL string without padding, safe for use in URLs and cookies.
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Computes SHA-256 hash of the input string and returns it as a 64-character lowercase hex string.
    /// Uses UTF-8 encoding so the hash is deterministic across all platforms.
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }
}
