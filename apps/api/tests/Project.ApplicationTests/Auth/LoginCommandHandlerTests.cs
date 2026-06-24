using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Application.Auth;
using Project.Application.Common;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.ApplicationTests.Auth;

public sealed class LoginCommandHandlerTests
{
    // ─────────────── Success ───────────────

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenPair()
    {
        // ARRANGE
        var clock = new FixedClock();
        var user = CreateUser("test@example.com", "hashed-pass", active: true);
        var roles = new List<Role> { CreateRole("admin") };
        var repo = new FakeUserRepository
        {
            User = user,
            Roles = roles,
        };
        var hasher = new FakePasswordHasher { VerifyResult = true };
        var jwt = new FakeJwtTokenService
        {
            AccessToken = "jwt.access.token",
            Lifetime = TimeSpan.FromMinutes(15),
            RefreshRaw = "raw-refresh-token",
            RefreshHash = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
        };
        var refreshRepo = new FakeRefreshTokenRepository();
        var tokenSvc = new FakeTokenService();
        var handler = new LoginCommandHandler(repo, hasher, jwt, refreshRepo, clock, tokenSvc);
        var command = new LoginCommand("test@example.com", "StrongP@ss1!");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("jwt.access.token", result.Value.AccessToken);
        Assert.Equal(900, result.Value.ExpiresInSeconds);
        Assert.Single(refreshRepo.SavedTokens);
        Assert.Equal("abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789",
            refreshRepo.SavedTokens[0].TokenHash);
    }

    // ─────────────── User not found → same generic 401 ───────────────

    [Fact]
    public async Task Handle_UserNotFound_ReturnsInvalidCredentials()
    {
        // ARRANGE
        var repo = new FakeUserRepository { User = null, Roles = Array.Empty<Role>() };
        var handler = CreateHandler(repo);
        var command = new LoginCommand("nobody@example.com", "any-password");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, result.Error.Code);
    }

    // ─────────────── Wrong password → same generic 401 ───────────────

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        // ARRANGE
        var user = CreateUser("test@example.com", "hashed-pass", active: true);
        var repo = new FakeUserRepository { User = user, Roles = Array.Empty<Role>() };
        var hasher = new FakePasswordHasher { VerifyResult = false };
        var handler = CreateHandler(repo, hasher);
        var command = new LoginCommand("test@example.com", "WrongP@ss1!");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, result.Error.Code);
    }

    // ─────────────── Blocked user → same generic 401 ───────────────

    [Fact]
    public async Task Handle_BlockedUser_ReturnsInvalidCredentials()
    {
        // ARRANGE
        var user = CreateUser("blocked@example.com", "hashed-pass", active: false);
        var repo = new FakeUserRepository { User = user, Roles = Array.Empty<Role>() };
        var hasher = new FakePasswordHasher { VerifyResult = true };
        var handler = CreateHandler(repo, hasher);
        var command = new LoginCommand("blocked@example.com", "StrongP@ss1!");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT — blocked users must not leak account state
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, result.Error.Code);
        Assert.Equal("Invalid credentials.", result.Error.Message);
    }

    // ─────────────── Blocked user: same generic message as wrong password ───────────────
    [Fact]
    public async Task Handle_BlockedUser_SameErrorAsWrongPassword()
    {
        // ARRANGE
        var user = CreateUser("blocked2@example.com", "hashed-pass", active: false);
        var repo = new FakeUserRepository { User = user, Roles = Array.Empty<Role>() };
        var hasher = new FakePasswordHasher { VerifyResult = true };
        var handler = CreateHandler(repo, hasher);

        // ACT
        var blockedResult = await handler.Handle(new LoginCommand("blocked2@example.com", "StrongP@ss1!"), CancellationToken.None);

        // Create a wrong-password scenario for comparison
        var activeUser = CreateUser("active@example.com", "hashed-pass", active: true);
        var wrongPwRepo = new FakeUserRepository { User = activeUser, Roles = Array.Empty<Role>() };
        var wrongPwHasher = new FakePasswordHasher { VerifyResult = false };
        var wrongPwHandler = CreateHandler(wrongPwRepo, wrongPwHasher);
        var wrongPwResult = await wrongPwHandler.Handle(new LoginCommand("active@example.com", "WrongP@ss1!"), CancellationToken.None);

        // ASSERT — error code AND message must be identical (no enumeration)
        Assert.Equal(wrongPwResult.Error.Code, blockedResult.Error.Code);
        Assert.Equal(wrongPwResult.Error.Message, blockedResult.Error.Message);
    }

    // ─────────────── Weak password → same generic 401 ───────────────

    [Fact]
    public async Task Handle_WeakPassword_ReturnsInvalidCredentials()
    {
        // ARRANGE
        var user = CreateUser("test@example.com", "hashed-pass", active: true);
        var repo = new FakeUserRepository { User = user, Roles = Array.Empty<Role>() };
        var hasher = new FakePasswordHasher { VerifyResult = true };
        var handler = CreateHandler(repo, hasher);
        var command = new LoginCommand("test@example.com", "short");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Auth.InvalidCredentials, result.Error.Code);
    }

    // ─────────────── Helpers ───────────────

    private static LoginCommandHandler CreateHandler(
        IUserRepository? repo = null,
        IPasswordHasher? hasher = null,
        IJwtTokenService? jwt = null,
        IRefreshTokenRepository? refreshRepo = null,
        IClock? clock = null,
        ITokenService? tokenSvc = null)
    {
        var defaultUser = CreateUser("test@example.com", "hashed-pass", active: true);
        return new LoginCommandHandler(
            repo ?? new FakeUserRepository { User = defaultUser, Roles = Array.Empty<Role>() },
            hasher ?? new FakePasswordHasher { VerifyResult = true },
            jwt ?? new FakeJwtTokenService
            {
                AccessToken = "jwt.token",
                Lifetime = TimeSpan.FromMinutes(15),
                RefreshRaw = "raw",
                RefreshHash = "aa".PadRight(64, 'a'),
            },
            refreshRepo ?? new FakeRefreshTokenRepository(),
            clock ?? new FixedClock(),
            tokenSvc ?? new FakeTokenService());
    }

    private static User CreateUser(string email, string passwordHash, bool active)
    {
        var user = User.Create(Email.Create(email), passwordHash, "system", new FixedClock());
        if (!active)
        {
            // Deactivate: pass empty active superadmins so guard passes
            user.Deactivate("system", new FixedClock(), Array.Empty<User>());
        }
        return user;
    }

    private static Role CreateRole(string name, bool isSystem = false)
    {
        return Role.Create(name, isSystem, "system", new FixedClock());
    }

    // ─────────────── Fakes ───────────────

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; set; }
        public IReadOnlyCollection<Role> Roles { get; set; } = Array.Empty<Role>();

        public Task<(User? User, IReadOnlyCollection<Role> Roles)> GetByEmailWithRolesAsync(
            Email email, CancellationToken ct = default)
        {
            return Task.FromResult((User, Roles));
        }

        public Task<(User? User, IReadOnlyCollection<Role> Roles)> GetByIdWithRolesAsync(
            UserId id, CancellationToken ct = default)
        {
            return Task.FromResult((User, Roles));
        }

        // Unused IBaseRepository members
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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public bool VerifyResult { get; set; } = true;
        public string Hash(string plaintext) => "hashed:" + plaintext;
        public bool Verify(string plaintext, string hash) => VerifyResult;
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string AccessToken { get; set; } = "fake-token";
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(15);
        public string RefreshRaw { get; set; } = "raw-refresh";
        public string RefreshHash { get; set; } = "aa".PadRight(64, 'a');

        public (string Token, TimeSpan Lifetime) GenerateAccessToken(User user, IReadOnlyCollection<Role> roles)
            => (AccessToken, Lifetime);

        public (string Raw, string Hash) GenerateRefreshToken()
            => (RefreshRaw, RefreshHash);

        public System.Security.Claims.ClaimsPrincipal? ValidateAccessToken(string token)
            => new();
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> SavedTokens { get; } = new();

        public Task AddAsync(RefreshToken entity, CancellationToken ct = default)
        {
            SavedTokens.Add(entity);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(
            Guid familyId, CancellationToken ct = default)
            => throw new NotImplementedException();

        // Unused IBaseRepository members
        public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<RefreshToken>> GetAllAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public void Update(RefreshToken entity) => throw new NotImplementedException();
        public void Delete(RefreshToken entity) => throw new NotImplementedException();
        public Task<PagedResult<RefreshToken>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default)
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
