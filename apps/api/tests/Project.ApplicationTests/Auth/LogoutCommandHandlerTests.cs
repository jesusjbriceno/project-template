using System.Security.Cryptography;
using System.Text;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Application.Auth;
using Project.Application.Common;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.ApplicationTests.Auth;

public sealed class LogoutCommandHandlerTests
{
    private static readonly UserId _testUserId = UserId.New();

    // ─────────────── Success ───────────────

    [Fact]
    public async Task Handle_ValidToken_RevokesTokenAndReturnsSuccess()
    {
        // ARRANGE
        var clock = new FixedClock();
        var existingToken = CreateRefreshToken(
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", clock, Guid.NewGuid());
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var handler = new LogoutCommandHandler(refreshRepo, clock);

        var command = new LogoutCommand("raw-token-to-revoke");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsSuccess);
        Assert.True(existingToken.IsRevoked);
        // F7: Verify exact SHA-256 hash of the raw token
        Assert.NotNull(refreshRepo.LastRequestedHash);
        var expectedHash = ComputeSha256Hash("raw-token-to-revoke");
        Assert.Equal(expectedHash, refreshRepo.LastRequestedHash);
    }

    // ─────────────── Token not found → success (idempotent) ───────────────

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsSuccess()
    {
        // ARRANGE
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = null };
        var handler = new LogoutCommandHandler(refreshRepo, new FixedClock());

        var command = new LogoutCommand("raw-nonexistent-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsSuccess);
    }

    // ─────────────── Already revoked token → success (idempotent) ───────────────

    [Fact]
    public async Task Handle_AlreadyRevokedToken_ReturnsSuccess()
    {
        // ARRANGE
        var clock = new FixedClock();
        var existingToken = CreateRefreshToken(
            "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb", clock, Guid.NewGuid());
        existingToken.Revoke(clock);
        var refreshRepo = new FakeRefreshTokenRepository { ExistingToken = existingToken };
        var handler = new LogoutCommandHandler(refreshRepo, clock);

        var command = new LogoutCommand("raw-already-revoked-token");

        // ACT
        var result = await handler.Handle(command, CancellationToken.None);

        // ASSERT
        Assert.True(result.IsSuccess);
    }

    // ─────────────── Helpers ───────────────

    private static string ComputeSha256Hash(string raw)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hashBytes);
    }

    private static RefreshToken CreateRefreshToken(string hash, IClock clock, Guid familyId)
    {
        return RefreshToken.Create(_testUserId, hash, familyId, clock.UtcNow.AddDays(7), clock);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public RefreshToken? ExistingToken { get; set; }
        public string? LastRequestedHash { get; private set; }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        {
            LastRequestedHash = tokenHash;
            return Task.FromResult(ExistingToken);
        }

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AddAsync(RefreshToken entity, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(
            Guid familyId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<RefreshToken>> GetAllAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public void Update(RefreshToken entity) => throw new NotImplementedException();
        public void Delete(RefreshToken entity) => throw new NotImplementedException();
        public Task<PagedResult<RefreshToken>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}
