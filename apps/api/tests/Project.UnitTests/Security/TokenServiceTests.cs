using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Security;

namespace Project.UnitTests.Security;

public class TokenServiceTests
{
    [Fact]
    public async Task RevokeFamilyAsync_CallsRepositoryWithCorrectFamilyId()
    {
        // ARRANGE
        var familyId = Guid.NewGuid();
        var repo = new FakeRefreshTokenRepository();
        var service = new TokenService(repo);

        // ACT
        await service.RevokeFamilyAsync(familyId);

        // ASSERT
        Assert.True(repo.RevokeFamilyCalled);
        Assert.Equal(familyId, repo.LastRevokedFamilyId);
    }

    [Fact]
    public async Task RevokeFamilyAsync_PropagatesCancellationToken()
    {
        // ARRANGE
        var repo = new FakeRefreshTokenRepository();
        var service = new TokenService(repo);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // ACT — cancellation should flow to the repository
        await service.RevokeFamilyAsync(Guid.NewGuid(), cts.Token);

        // ASSERT
        Assert.True(repo.LastCancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task RevokeFamilyAsync_ThrowsWhenRepositoryThrows()
    {
        // ARRANGE
        var repo = new FakeRefreshTokenRepository { ShouldThrow = true };
        var service = new TokenService(repo);

        // ACT & ASSERT
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RevokeFamilyAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Fake implementation of <see cref="IRefreshTokenRepository"/> for adapter testing.
    /// Only implements the method needed by <see cref="ITokenService"/>.
    /// </summary>
    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public bool RevokeFamilyCalled { get; private set; }
        public Guid LastRevokedFamilyId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public bool ShouldThrow { get; set; }

        public Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default)
        {
            if (ShouldThrow) throw new InvalidOperationException("Test exception");

            RevokeFamilyCalled = true;
            LastRevokedFamilyId = familyId;
            LastCancellationToken = ct;
            return Task.CompletedTask;
        }

        // Remaining interface members — not used by TokenService
        public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, bool includeDeleted = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<PagedResult<RefreshToken>> GetPagedAsync(PageRequest request, bool includeDeleted = false, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task AddAsync(RefreshToken entity, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public void Update(RefreshToken entity) => throw new NotImplementedException();
        public void Delete(RefreshToken entity) => throw new NotImplementedException();
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<IReadOnlyCollection<RefreshToken>> GetActiveByFamilyIdAsync(Guid familyId, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}
