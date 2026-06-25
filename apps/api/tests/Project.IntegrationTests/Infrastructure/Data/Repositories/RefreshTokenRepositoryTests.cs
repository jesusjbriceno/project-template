using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Repositories;

namespace Project.IntegrationTests.Infrastructure.Data.Repositories;

[Collection("Postgres")]
public sealed class RefreshTokenRepositoryTests : IClassFixture<PostgresFixture>
{
    private static readonly UserId _testUserId = UserId.New();
    private readonly PostgresFixture _fixture;
    public RefreshTokenRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    private static string Hash(int seed) => string.Create(64, seed, (s, i) =>
    { for (int j = 0; j < 64; j++) { var v = (i * 31 + j) % 16; s[j] = (char)(v < 10 ? '0' + v : 'a' + v - 10); } });
    private static async Task CleanDb(PostgresFixture f)
    {
        using var scope = f.CreateServiceProvider().CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .RefreshTokens.ExecuteDeleteAsync();
    }
    private async Task<(ApplicationDbContext Ctx, RefreshTokenRepository Repo)> Setup()
    {
        var scope = _fixture.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (ctx, new RefreshTokenRepository(ctx));
    }

    [Fact]
    public async Task GetByTokenHashAsync_FindsByHash()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var hash = Hash(1);
        ctx.RefreshTokens.Add(RefreshToken.Create(_testUserId, hash, Guid.NewGuid(), clock.UtcNow.AddDays(7), clock));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        var found = await repo.GetByTokenHashAsync(hash);
        Assert.NotNull(found);
        Assert.Equal(hash, found!.TokenHash);
    }

    [Fact]
    public async Task GetByTokenHashAsync_NotFound_ReturnsNull()
    {
        await CleanDb(_fixture);
        var (_, repo) = await Setup();
        Assert.Null(await repo.GetByTokenHashAsync(Hash(99)));
    }

    [Fact]
    public async Task GetActiveByFamilyIdAsync_ReturnsOnlyActiveNonRevoked()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var fid = Guid.NewGuid();
        var future = clock.UtcNow.AddDays(7);
        var active = RefreshToken.Create(_testUserId, Hash(10), fid, future, clock);
        var revoked = RefreshToken.Create(_testUserId, Hash(11), fid, future, clock);
        revoked.Revoke(clock);
        var expired = RefreshToken.Create(_testUserId, Hash(12), fid, clock.UtcNow.AddDays(-1), clock);
        ctx.RefreshTokens.AddRange(active, revoked, expired);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        var result = await repo.GetActiveByFamilyIdAsync(fid);
        Assert.Single(result);
        Assert.Equal(active.Id, result.First().Id);
    }

    [Fact]
    public async Task GetActiveByFamilyIdAsync_DifferentFamily_NotReturned()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var future = clock.UtcNow.AddDays(7);
        var ta = RefreshToken.Create(_testUserId, Hash(20), Guid.NewGuid(), future, clock);
        ctx.RefreshTokens.AddRange(ta, RefreshToken.Create(_testUserId, Hash(21), Guid.NewGuid(), future, clock));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        Assert.Single(await repo.GetActiveByFamilyIdAsync(ta.FamilyId));
    }

    [Fact]
    public async Task RevokeFamilyAsync_RevokesAllInFamily()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var fid = Guid.NewGuid();
        var future = clock.UtcNow.AddDays(7);
        ctx.RefreshTokens.AddRange(
            RefreshToken.Create(_testUserId, Hash(30), fid, future, clock),
            RefreshToken.Create(_testUserId, Hash(31), fid, future, clock));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        await repo.RevokeFamilyAsync(fid);
        var reloaded = await ctx.RefreshTokens.Where(rt => rt.FamilyId == fid).ToListAsync();
        Assert.Equal(2, reloaded.Count);
        Assert.All(reloaded, rt => Assert.True(rt.IsRevoked));
    }

    [Fact]
    public async Task RevokeFamilyAsync_IgnoresOtherFamily()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var future = clock.UtcNow.AddDays(7);
        var ta = RefreshToken.Create(_testUserId, Hash(40), Guid.NewGuid(), future, clock);
        ctx.RefreshTokens.AddRange(ta, RefreshToken.Create(_testUserId, Hash(41), Guid.NewGuid(), future, clock));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        await repo.RevokeFamilyAsync(ta.FamilyId);
        Assert.True((await ctx.RefreshTokens.FirstAsync(rt => rt.Id == ta.Id)).IsRevoked);
    }
}
