using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Project.Application.Abstractions.Security;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Security;

namespace Project.UnitTests.Security;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions ValidOptions = new()
    {
        Secret = "this-is-a-very-secure-secret-key-with-32-plus-bytes!!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        RefreshTokenDays = 7,
    };

    private static readonly TestClock FixedClock = new();

    private static JwtTokenService CreateService() => new(ValidOptions, FixedClock);

    private static User CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email, "hashed-password", "system", new TestClock());
    }

    private static Role CreateTestRole(string name, bool isSystem = false)
    {
        return Role.Create(name, isSystem, "system", new TestClock());
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken_And_ValidateReturnsPrincipal()
    {
        // ARRANGE
        var service = CreateService();
        var user = CreateTestUser();
        var roles = new List<Role> { CreateTestRole("admin") };

        // ACT
        var (token, lifetime) = service.GenerateAccessToken(user, roles);

        // ASSERT — token is non-empty
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(lifetime > TimeSpan.Zero);

        // Validate the token and verify claims
        var principal = service.ValidateAccessToken(token);
        Assert.NotNull(principal);

        // Verify standard claims — MapInboundClaims=false preserves original JWT claim types
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        Assert.Equal(user.Id.ToString(), sub);

        var emailClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
        Assert.Equal(user.Email.Value, emailClaim);

        var rolesClaim = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("admin", rolesClaim);
    }

    [Fact]
    public void ValidateAccessToken_TamperedToken_ReturnsNull()
    {
        // ARRANGE
        var service = CreateService();
        var user = CreateTestUser();
        var roles = new List<Role> { CreateTestRole("admin") };

        var (token, _) = service.GenerateAccessToken(user, roles);

        // Tamper: append a character
        var tampered = token + "x";

        // ACT
        var principal = service.ValidateAccessToken(tampered);

        // ASSERT
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateAccessToken_WithRoles_IncludesRolesClaim()
    {
        // ARRANGE
        var service = CreateService();
        var user = CreateTestUser();
        var roles = new List<Role>
        {
            CreateTestRole("admin"),
            CreateTestRole("editor"),
        };

        // ACT
        var (token, _) = service.GenerateAccessToken(user, roles);
        var principal = service.ValidateAccessToken(token);

        // ASSERT
        Assert.NotNull(principal);
        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("admin", roleClaims);
        Assert.Contains("editor", roleClaims);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDistinctRawAndHash()
    {
        // ARRANGE
        var service = CreateService();

        // ACT
        var (raw, hash) = service.GenerateRefreshToken();

        // ASSERT
        Assert.False(string.IsNullOrWhiteSpace(raw));
        Assert.Equal(64, hash.Length); // SHA-256 hex is 64 chars
        Assert.NotEqual(raw, hash); // raw and hash must differ

        // Hash must be valid lowercase hex
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueTokens()
    {
        // ARRANGE
        var service = CreateService();

        // ACT
        var (raw1, hash1) = service.GenerateRefreshToken();
        var (raw2, hash2) = service.GenerateRefreshToken();

        // ASSERT — each call produces different raw and hash values
        Assert.NotEqual(raw1, raw2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ValidateAccessToken_InvalidFormat_ReturnsNull()
    {
        // ARRANGE
        var service = CreateService();

        // ACT
        var principal = service.ValidateAccessToken("not-a-valid-jwt-token-at-all");

        // ASSERT
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateAccessToken_EmptyOrWhitespace_ReturnsNull()
    {
        // ARRANGE
        var service = CreateService();

        // ACT & ASSERT
        Assert.Null(service.ValidateAccessToken(string.Empty));
        Assert.Null(service.ValidateAccessToken("   "));
    }

    [Fact]
    public void ValidateAccessToken_WrongSigningKey_ReturnsNull()
    {
        // ARRANGE
        var serviceA = CreateService();
        var user = CreateTestUser();
        var roles = new List<Role> { CreateTestRole("admin") };

        var (token, _) = serviceA.GenerateAccessToken(user, roles);

        // Service with a different secret
        var otherOptions = new JwtOptions
        {
            Secret = "a-different-secret-key-that-must-be-at-least-32-bytes!!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            RefreshTokenDays = 7,
        };
        var serviceB = new JwtTokenService(otherOptions, FixedClock);

        // ACT — serviceB validates serviceA's token (different key)
        var principal = serviceB.ValidateAccessToken(token);

        // ASSERT
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateRefreshToken_HashIsSha256OfRawString()
    {
        // ARRANGE
        var service = CreateService();

        // ACT
        var (raw, hash) = service.GenerateRefreshToken();

        // ASSERT — independently compute SHA-256 of the raw (base64url) string.
        // This proves the stored hash can be recomputed from the client-provided raw token.
        var rawBytes = Encoding.UTF8.GetBytes(raw);
        var expectedHashBytes = SHA256.HashData(rawBytes);
        var expectedHash = Convert.ToHexStringLower(expectedHashBytes);

        Assert.Equal(64, expectedHash.Length);
        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public void ValidateAccessToken_ExpiredToken_ReturnsNull()
    {
        // ARRANGE — use a mutable clock so the token can "expire" deterministically
        var clock = new MutableClock(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new JwtTokenService(ValidOptions, clock);
        var user = CreateTestUser();
        var roles = new List<Role> { CreateTestRole("admin") };

        // Generate token at T=12:00 (expires 12:15)
        var (token, _) = service.GenerateAccessToken(user, roles);

        // Still valid immediately after issuance
        Assert.NotNull(service.ValidateAccessToken(token));

        // Advance clock past expiry (T=12:16)
        clock.SetUtcNow(new DateTimeOffset(2026, 6, 1, 12, 16, 0, TimeSpan.Zero));

        // ACT — validation must reject the expired token
        var principal = service.ValidateAccessToken(token);

        // ASSERT
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateAccessToken_WrongIssuer_ReturnsNull()
    {
        // ARRANGE — generate token with issuer "test-issuer"
        var service = CreateService();
        var user = CreateTestUser();
        var roles = new List<Role> { CreateTestRole("admin") };

        var (token, _) = service.GenerateAccessToken(user, roles);

        // Validate with a service configured for a different issuer (same key, same audience)
        var otherOptions = new JwtOptions
        {
            Secret = ValidOptions.Secret,
            Issuer = "wrong-issuer",
            Audience = ValidOptions.Audience,
            RefreshTokenDays = 7,
        };
        var otherService = new JwtTokenService(otherOptions, FixedClock);

        // ACT
        var principal = otherService.ValidateAccessToken(token);

        // ASSERT — issuer mismatch must cause validation failure
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateAccessToken_WrongAudience_ReturnsNull()
    {
        // ARRANGE — generate token with audience "test-audience"
        var service = CreateService();
        var user = CreateTestUser();
        var roles = new List<Role> { CreateTestRole("admin") };

        var (token, _) = service.GenerateAccessToken(user, roles);

        // Validate with a service configured for a different audience (same key, same issuer)
        var otherOptions = new JwtOptions
        {
            Secret = ValidOptions.Secret,
            Issuer = ValidOptions.Issuer,
            Audience = "wrong-audience",
            RefreshTokenDays = 7,
        };
        var otherService = new JwtTokenService(otherOptions, FixedClock);

        // ACT
        var principal = otherService.ValidateAccessToken(token);

        // ASSERT — audience mismatch must cause validation failure
        Assert.Null(principal);
    }

    /// <summary>
    /// Simple fake clock returning a fixed UTC time for tests.
    /// </summary>
    private sealed class TestClock : Project.Domain.Common.IClock
    {
        public DateTimeOffset UtcNow => new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    }

    /// <summary>
    /// Mutable fake clock for testing time-dependent behavior like token expiry.
    /// </summary>
    private sealed class MutableClock : Project.Domain.Common.IClock
    {
        private DateTimeOffset _utcNow;

        public MutableClock(DateTimeOffset initial) => _utcNow = initial;

        public DateTimeOffset UtcNow => _utcNow;

        public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
    }
}
