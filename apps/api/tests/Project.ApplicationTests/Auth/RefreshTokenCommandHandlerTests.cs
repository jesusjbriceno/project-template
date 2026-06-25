using System.Security.Cryptography;
using System.Text;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Application.Auth;
using Project.Application.Common;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.Errors;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.ApplicationTests.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly UserId _testUserId = UserId.New();
    private static readonly AuthTokenOptions DefaultOptions = new() { RefreshTokenDays = 7 };

    // ─────────────── Success (rotation) ───────────────

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokenPair()
    {
        // ARRANGE
        var clock = new FixedClock();
        var user = CreateUser("test@example.com");
        var roles = new List<Role> { CreateRole("admin") };
        var existingToken = CreateRefreshToken("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", clock, Guid.NewGuid());
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var handler = CreateHandler(clock, refreshRepo, user, roles, existingToken);

        var command = new RefreshTokenCommand("some-raw-refresh-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error.Code} - {result.Error.Message}");
        Assert.NotNull(result.Value);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal(900, result.Value.ExpiresInSeconds);
        // F7: Verify the handler computed exact SHA-256 hash of the raw token
        Assert.NotNull(refreshRepo.LastRequestedHash);
        var expectedHash = ComputeSha256Hash("some-raw-refresh-token");
        Assert.Equal(expectedHash, refreshRepo.LastRequestedHash);
        // Old token revoked, new token stored
        Assert.True(existingToken.IsRevoked);
        Assert.Single(refreshRepo.SavedTokens);
    }

    // ─────────────── Expired token ───────────────

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsTokenExpired()
    {
        // ARRANGE
        var clock = new FixedClock();
        var pastExpiry = clock.UtcNow.AddDays(-14);
        var expiredToken = CreateRefreshToken("ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb", clock, Guid.NewGuid(), pastExpiry);
        var handler = CreateHandler(clock, existingToken: expiredToken);

        var command = new RefreshTokenCommand("raw-expired-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.TokenExpired, result.Error.Code);
    }

    // ─────────────── Token reuse → family revoke ───────────────

    [Fact]
    public async Task Handle_ReusedToken_RevokesFamilyAndReturnsTokenReuseDetected()
    {
        // ARRANGE
        var clock = new FixedClock();
        var existingToken = CreateRefreshToken("3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d", clock, Guid.NewGuid());
        // Revoke the token first (simulating prior revocation)
        existingToken.Revoke(clock);

        var tokenSvc = new FakeTokenService();
        var handler = CreateHandler(clock, tokenSvc: tokenSvc, existingToken: existingToken);

        var command = new RefreshTokenCommand("raw-reused-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.TokenReuseDetected, result.Error.Code);
        Assert.Equal(1, tokenSvc.RevokeFamilyCallCount);
    }

    // ─────────────── Token not found ───────────────

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsTokenExpired()
    {
        // ARRANGE
        var handler = CreateHandler(new FixedClock(), existingToken: null);

        var command = new RefreshTokenCommand("raw-nonexistent-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.TokenExpired, result.Error.Code);
    }

    // ─────────────── F1: Old token revocation must be persisted ───────────────

    [Fact]
    public async Task Handle_ValidToken_PersistsOldTokenRevocationViaUpdate()
    {
        // ARRANGE
        var clock = new FixedClock();
        var user = CreateUser("test@example.com");
        var roles = new List<Role> { CreateRole("admin") };
        var existingToken = CreateRefreshToken("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", clock, Guid.NewGuid());
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var handler = CreateHandler(clock, refreshRepo, user, roles, existingToken);

        var command = new RefreshTokenCommand("some-raw-refresh-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT — old token MUST be passed to Update() so its RevokedAt/ReplacedByTokenHash are persisted
        Assert.True(result.IsSuccess);
        Assert.NotNull(refreshRepo.LastUpdated);
        Assert.Same(existingToken, refreshRepo.LastUpdated);
        Assert.True(existingToken.IsRevoked);
        Assert.NotNull(existingToken.ReplacedByTokenHash);
    }

    // ─────────────── F5: No mutation before user check ───────────────

    [Fact]
    public async Task Handle_BlockedUser_DoesNotPersistNewTokenAndDoesNotMutateOldToken()
    {
        // ARRANGE
        var clock = new FixedClock();
        // Create a user that IS blocked
        var blockedUser = CreateUser("blocked@example.com");
        blockedUser.Deactivate("test", clock, Array.Empty<User>());
        var roles = new List<Role> { CreateRole("viewer") };
        var userId = _testUserId;
        var existingToken = CreateRefreshToken("3e23e8160039594a33894f6564e1b1348bbd7a0088d42c4acb73eeaed59c009d", clock, Guid.NewGuid());
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var userRepo = new FakeUserRepository { User = blockedUser, Roles = roles };
        var handler = new RefreshTokenCommandHandler(
            refreshRepo,
            new FakeJwtTokenService
            {
                AccessToken = "new-access-token",
                Lifetime = TimeSpan.FromMinutes(15),
                RefreshRaw = "new-raw-refresh",
                RefreshHash = "0e".PadRight(64, '0'),
            },
            clock,
            new FakeTokenService(),
            userRepo,
            DefaultOptions);

        var command = new RefreshTokenCommand("raw-for-blocked-user");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT — must not mutate anything when user is blocked
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, result.Error.Code);
        // No new token stored
        Assert.Empty(refreshRepo.SavedTokens);
        // No Update called on existing token
        Assert.Null(refreshRepo.LastUpdated);
        // Old token is NOT revoked
        Assert.False(existingToken.IsRevoked);
    }

    [Fact]
    public async Task Handle_MissingUser_DoesNotPersistNewToken()
    {
        // ARRANGE
        var clock = new FixedClock();
        var existingToken = CreateRefreshToken("ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb", clock, Guid.NewGuid());
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var userRepo = new FakeUserRepository { User = null, Roles = Array.Empty<Role>() };
        var handler = new RefreshTokenCommandHandler(
            refreshRepo,
            new FakeJwtTokenService
            {
                AccessToken = "new-access-token",
                Lifetime = TimeSpan.FromMinutes(15),
                RefreshRaw = "new-raw-refresh",
                RefreshHash = "0e".PadRight(64, '0'),
            },
            clock,
            new FakeTokenService(),
            userRepo,
            DefaultOptions);

        var command = new RefreshTokenCommand("raw-for-deleted-user");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT — must not persist when user is missing
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, result.Error.Code);
        Assert.Empty(refreshRepo.SavedTokens);
        Assert.Null(refreshRepo.LastUpdated);
        Assert.False(existingToken.IsRevoked);
    }

    // ─────────────── Configurable refresh token expiry ───────────────

    [Fact]
    public async Task Handle_ConfigurableRefreshTokenExpiry_UsesAuthTokenOptions()
    {
        // ARRANGE
        var clock = new FixedClock();
        var user = CreateUser("test@example.com");
        var roles = new List<Role> { CreateRole("admin") };
        var existingToken = CreateRefreshToken("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", clock, Guid.NewGuid());
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var nonDefaultOptions = new AuthTokenOptions { RefreshTokenDays = 14 };
        var handler = CreateHandler(clock, refreshRepo, user, roles, existingToken, authOptions: nonDefaultOptions);

        var command = new RefreshTokenCommand("some-raw-refresh-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT — the rotated token expiry must respect AuthTokenOptions.RefreshTokenDays
        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error.Code} - {result.Error.Message}");
        Assert.Single(refreshRepo.SavedTokens);
        var savedToken = refreshRepo.SavedTokens[0];
        var expectedExpiry = clock.UtcNow.AddDays(14);
        Assert.Equal(expectedExpiry, savedToken.ExpiresAt);
    }

    // ─────────────── Helpers ───────────────

    private static string ComputeSha256Hash(string raw)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hashBytes);
    }

    private static RefreshTokenCommandHandler CreateHandler(
        IClock? clock = null,
        IRefreshTokenRepository? refreshRepo = null,
        User? user = null,
        IReadOnlyCollection<Role>? roles = null,
        RefreshToken? existingToken = null,
        ITokenService? tokenSvc = null,
        AuthTokenOptions? authOptions = null)
    {
        user ??= CreateUser("test@example.com");
        roles ??= Array.Empty<Role>();
        return new RefreshTokenCommandHandler(
            refreshRepo ?? new FakeRefreshTokenRepository { ExistingToken = existingToken },
            new FakeJwtTokenService
            {
                AccessToken = "new-access-token",
                Lifetime = TimeSpan.FromMinutes(15),
                RefreshRaw = "new-raw-refresh",
                RefreshHash = "0e".PadRight(64, '0'),
            },
            clock ?? new FixedClock(),
            tokenSvc ?? new FakeTokenService(),
            new FakeUserRepository { User = user, Roles = roles },
            authOptions ?? DefaultOptions);
    }

    private static User CreateUser(string email)
    {
        return User.Create(Email.Create(email), "hashed-password", "system", new FixedClock());
    }

    private static Role CreateRole(string name)
    {
        return Role.Create(name, false, "system", new FixedClock());
    }

    private static RefreshToken CreateRefreshToken(string hash, IClock clock, Guid familyId, DateTimeOffset? expiresAt = null)
    {
        return RefreshToken.Create(_testUserId, hash, familyId, expiresAt ?? clock.UtcNow.AddDays(7), clock);
    }

    // ─────────────── Fakes ───────────────

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshToken? ExistingToken { get; set; }
        public List<RefreshToken> SavedTokens { get; } = new();
        public RefreshToken? LastUpdated { get; private set; }
        public string? LastRequestedHash { get; private set; }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        {
            LastRequestedHash = tokenHash;
            return Task.FromResult(ExistingToken);
        }

        public Task AddAsync(RefreshToken entity, CancellationToken ct = default)
        {
            SavedTokens.Add(entity);
            return Task.CompletedTask;
        }

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(
            Guid familyId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyCollection<RefreshToken>>(Array.Empty<RefreshToken>());

        public void Update(RefreshToken entity)
        {
            LastUpdated = entity;
        }

        public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<RefreshToken>> GetAllAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public void Delete(RefreshToken entity) => throw new NotImplementedException();
        public Task<PagedResult<RefreshToken>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string AccessToken { get; set; } = "token";
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(15);
        public string RefreshRaw { get; set; } = "raw";
        public string RefreshHash { get; set; } = "aa".PadRight(64, 'a');

        public (string Token, TimeSpan Lifetime) GenerateAccessToken(User user, IReadOnlyCollection<Role> roles)
            => (AccessToken, Lifetime);
        public (string Raw, string Hash) GenerateRefreshToken()
            => (RefreshRaw, RefreshHash);
        public System.Security.Claims.ClaimsPrincipal? ValidateAccessToken(string token)
            => new();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; set; }
        public IReadOnlyCollection<Role> Roles { get; set; } = Array.Empty<Role>();

        public Task<(User? User, IReadOnlyCollection<Role> Roles)> GetByIdWithRolesAsync(
            UserId id, CancellationToken ct = default)
            => Task.FromResult((User, Roles));

        public Task<(User? User, IReadOnlyCollection<Role> Roles)> GetByEmailWithRolesAsync(
            Email email, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<User?> GetByIdAsync(UserId id, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task AddAsync(User entity, CancellationToken ct = default)
            => throw new NotImplementedException();
        public void Update(User entity) => throw new NotImplementedException();
        public void Delete(User entity) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<bool> ExistsAsync(UserId id, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<User>> GetActiveSuperadminsAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeTokenService : ITokenService
    {
        public int RevokeFamilyCallCount { get; private set; }
        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
        {
            RevokeFamilyCallCount++;
            return Task.CompletedTask;
        }
    }
}
