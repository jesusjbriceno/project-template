using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Domain.Common;
using Project.Domain.Errors;

namespace Project.UnitTests.Entities;

public class RefreshTokenTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);
    }

    // Valid SHA-256 hex hashes (exactly 64 lowercase hex characters)
    // SHA256("")       = e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
    // SHA256("a")      = ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb
    // SHA256("b")      = 3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d
    private const string Hash1 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const string Hash2 = "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb";
    private const string Hash3 = "3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d";

    // --- Finding 1: SHA-256 hash validation ---

    [Fact]
    public void Create_WithValidData_CreatesToken()
    {
        var clock = new FakeClock();
        var expiresAt = clock.UtcNow.AddDays(7);

        var token = RefreshToken.Create(Hash1, Guid.NewGuid(), expiresAt, clock);

        Assert.NotEqual(default, token.Id);
        Assert.Equal(Hash1, token.TokenHash);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Equal(clock.UtcNow, token.CreatedAt);
        Assert.False(token.IsRevoked);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenHash);
    }

    [Fact]
    public void Create_WithNullTokenHash_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentNullException>(() =>
            RefreshToken.Create(null!, Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("tokenHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithEmptyTokenHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create("", Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("tokenHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithWhitespaceTokenHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create("   ", Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("tokenHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithShortTokenHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create("abc123", Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("64", ex.Message);
        Assert.Contains("tokenHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithNonHexTokenHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var nonHex = new string('g', 64); // 64 'g' chars — 'g' is not hex
        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(nonHex, Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("hex", ex.Message.ToLowerInvariant());
        Assert.Contains("tokenHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithUppercaseHexTokenHash_NormalizesToLowercase()
    {
        var clock = new FakeClock();
        var upperHash = Hash1.ToUpperInvariant(); // same hash, uppercase

        var token = RefreshToken.Create(upperHash, Guid.NewGuid(), clock.UtcNow.AddDays(7), clock);

        Assert.Equal(Hash1, token.TokenHash); // normalized to lowercase
    }

    [Fact]
    public void Create_WithMixedCaseHexTokenHash_NormalizesToLowercase()
    {
        var clock = new FakeClock();
        // Take a valid lowercase hash and uppercase every other character
        var mixed = string.Create(64, Hash1, (span, hash) =>
        {
            for (int i = 0; i < hash.Length; i++)
                span[i] = i % 2 == 0 ? char.ToUpperInvariant(hash[i]) : hash[i];
        });

        var token = RefreshToken.Create(mixed, Guid.NewGuid(), clock.UtcNow.AddDays(7), clock);

        Assert.Equal(Hash1, token.TokenHash); // fully normalized to lowercase
    }

    [Fact]
    public void Create_WithLongTokenHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var longHash = Hash1 + "00"; // 66 chars

        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(longHash, Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("64", ex.Message);
        Assert.Contains("tokenHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithSpecialCharsInTokenHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        // 64 chars with a non-hex character in the middle
        var corrupt = Hash1[..32] + "@-" + Hash1[34..];

        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(corrupt, Guid.NewGuid(), clock.UtcNow, clock));
        Assert.Contains("hex", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void Rotate_ExpiredTokenAtExactBoundary_ThrowsInvalidOperationException()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow, clock); // expires exactly at UtcNow

        Assert.True(token.IsExpired(clock)); // boundary: expired

        var ex = Assert.Throws<InvalidOperationException>(() =>
            token.Rotate(Hash2, clock.UtcNow.AddDays(14), clock));
        Assert.Contains("expired", ex.Message.ToLowerInvariant());
    }

    // --- Finding 5: FamilyId must reject Guid.Empty ---

    [Fact]
    public void Create_WithEmptyFamilyId_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var ex = Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(Hash1, Guid.Empty, clock.UtcNow.AddDays(7), clock));
        Assert.Contains("familyId", ex.ParamName!);
    }

    // --- Existing lifecycle tests (updated to use valid hashes) ---

    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddMinutes(-10), clock);

        Assert.True(token.IsExpired(clock));
    }

    [Fact]
    public void IsExpired_BeforeExpiry_ReturnsFalse()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);

        Assert.False(token.IsExpired(clock));
    }

    [Fact]
    public void IsRevoked_FreshToken_ReturnsFalse()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);

        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void IsActive_FreshToken_ReturnsTrue()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);

        Assert.True(token.IsActive(clock));
    }

    [Fact]
    public void IsActive_ExpiredToken_ReturnsFalse()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddMinutes(-1), clock);

        Assert.False(token.IsActive(clock));
    }

    [Fact]
    public void IsActive_RevokedToken_ReturnsFalse()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);

        token.Revoke(clock);

        Assert.False(token.IsActive(clock));
    }

    [Fact]
    public void IsReuseSignal_FreshToken_ReturnsFalse()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);

        Assert.False(token.IsReuseSignal);
    }

    // --- Finding 3: Revoked-without-replacement is still a reuse signal ---

    [Fact]
    public void IsReuseSignal_RevokedWithoutReplacement_ReturnsTrue()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);
        token.Revoke(clock);

        Assert.True(token.IsRevoked);
        Assert.True(token.IsReuseSignal); // any revoked token signals reuse
    }

    // --- Rotation tests (updated to use valid hashes) ---

    [Fact]
    public void Rotate_RevokesCurrentToken()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        token.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        Assert.True(token.IsRevoked);
        Assert.Equal(clock.UtcNow, token.RevokedAt);
    }

    [Fact]
    public void Rotate_SetsReplacedByTokenHash()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        token.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        Assert.Equal(Hash2, token.ReplacedByTokenHash);
    }

    [Fact]
    public void Rotate_ReturnsNewTokenInSameFamily()
    {
        var clock = new FakeClock();
        var familyId = Guid.NewGuid();
        var t1 = RefreshToken.Create(Hash1, familyId,
            clock.UtcNow.AddDays(1), clock);

        var t2 = t1.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        Assert.NotNull(t2);
        Assert.Equal(familyId, t2.FamilyId);
        Assert.NotEqual(t1.Id, t2.Id);
    }

    [Fact]
    public void Rotate_NewTokenIsActive()
    {
        var clock = new FakeClock();
        var t1 = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        var t2 = t1.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        Assert.True(t2.IsActive(clock));
        Assert.False(t2.IsRevoked);
    }

    [Fact]
    public void IsReuseSignal_AfterRotation_ReturnsTrue()
    {
        var clock = new FakeClock();
        var t1 = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        t1.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        Assert.True(t1.IsReuseSignal);
    }

    // --- Finding 3: Rotate on revoked-without-replacement triggers reuse signal ---

    [Fact]
    public void Rotate_OnRevokedWithoutReplacementToken_ThrowsRefreshTokenReuseSignalException()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(7), clock);
        token.Revoke(clock); // admin/logout revocation — no replacement

        var ex = Assert.Throws<RefreshTokenReuseSignalException>(() =>
            token.Rotate(Hash2, clock.UtcNow.AddDays(14), clock));
        Assert.Contains("reuse", ex.Message.ToLowerInvariant());
        Assert.Contains("family", ex.Message.ToLowerInvariant());
    }

    // --- Finding 2: Expired tokens cannot be rotated ---

    [Fact]
    public void Rotate_OnExpiredToken_ThrowsInvalidOperationException()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddMinutes(-10), clock); // already expired

        Assert.True(token.IsExpired(clock));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            token.Rotate(Hash2, clock.UtcNow.AddDays(14), clock));
        Assert.Contains("expired", ex.Message.ToLowerInvariant());
    }

    // --- Finding 4: Mutate-after-validate ---

    [Fact]
    public void Rotate_WithInvalidNewTokenHash_DoesNotMutateCurrentToken()
    {
        var clock = new FakeClock();
        var token = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        var invalidHash = "not-a-valid-sha256-hash";

        try
        {
            token.Rotate(invalidHash, clock.UtcNow.AddDays(7), clock);
        }
        catch (ArgumentException)
        {
            // Expected — validation should fail before mutation
        }

        // Token must remain UNCHANGED
        Assert.False(token.IsRevoked);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ReplacedByTokenHash);
    }

    // --- Task 5.2: Chain tests (updated to use valid hashes) ---

    [Fact]
    public void Chain_T1RotateToT2_IsReuseSignalOnT1()
    {
        var clock = new FakeClock();
        var t1 = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        t1.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        Assert.True(t1.IsRevoked);
        Assert.Equal(Hash2, t1.ReplacedByTokenHash);
        Assert.True(t1.IsReuseSignal);
    }

    [Fact]
    public void Chain_T1ToT2ToT3_AllPredecessorsReplaced()
    {
        var clock = new FakeClock();
        var familyId = Guid.NewGuid();
        var t1 = RefreshToken.Create(Hash1, familyId,
            clock.UtcNow.AddDays(1), clock);
        var t2 = t1.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);
        var t3 = t2.Rotate(Hash3, clock.UtcNow.AddDays(14), clock);

        // T1: revoked, replaced by Hash2
        Assert.True(t1.IsReuseSignal);
        Assert.Equal(Hash2, t1.ReplacedByTokenHash);

        // T2: revoked, replaced by Hash3
        Assert.True(t2.IsReuseSignal);
        Assert.Equal(Hash3, t2.ReplacedByTokenHash);

        // T3: active (latest)
        Assert.True(t3.IsActive(clock));
        Assert.False(t3.IsRevoked);

        // All in same family
        Assert.Equal(familyId, t1.FamilyId);
        Assert.Equal(familyId, t2.FamilyId);
        Assert.Equal(familyId, t3.FamilyId);
    }

    [Fact]
    public void Chain_ReuseOfRotatedToken_ThrowsRefreshTokenReuseSignalException()
    {
        var clock = new FakeClock();
        var t1 = RefreshToken.Create(Hash1, Guid.NewGuid(),
            clock.UtcNow.AddDays(1), clock);

        // First rotation is fine
        t1.Rotate(Hash2, clock.UtcNow.AddDays(7), clock);

        // Presenting T1 again triggers reuse detection
        var ex = Assert.Throws<RefreshTokenReuseSignalException>(() =>
            t1.Rotate(Hash3, clock.UtcNow.AddDays(14), clock));
        Assert.Contains("reuse", ex.Message.ToLowerInvariant());
        Assert.Contains("family", ex.Message.ToLowerInvariant());
    }
}
