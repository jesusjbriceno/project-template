using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Repositories;

namespace Project.IntegrationTests.Infrastructure.Data.Repositories;

[Collection("Postgres")]
public sealed class PermissionRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public PermissionRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    private static async Task CleanDb(PostgresFixture f)
    {
        using var scope = f.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ctx.Permissions.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task GetByKeyAsync_ExistingKey_ReturnsPermission()
    {
        await CleanDb(_fixture);
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = new SystemClock();
        var key = PermissionKey.Create("users.read");
        ctx.Permissions.Add(Permission.Create(key, "Read users", "Users", "test", clock));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PermissionRepository(ctx);
        var result = await repo.GetByKeyAsync(key);

        Assert.NotNull(result);
        Assert.Equal(key, result!.Key);
    }

    [Fact]
    public async Task GetByKeyAsync_NonExistingKey_ReturnsNull()
    {
        await CleanDb(_fixture);
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repo = new PermissionRepository(ctx);
        Assert.Null(await repo.GetByKeyAsync(PermissionKey.Create("nope.missing")));
    }

    [Fact]
    public async Task ExistsByKeyAsync_ExistingKey_ReturnsTrue()
    {
        await CleanDb(_fixture);
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = new SystemClock();
        var key = PermissionKey.Create("admin.create");
        ctx.Permissions.Add(Permission.Create(key, "Create admin", "Admin", "test", clock));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        Assert.True(await new PermissionRepository(ctx).ExistsByKeyAsync(key));
    }

    [Fact]
    public async Task ExistsByKeyAsync_NonExistingKey_ReturnsFalse()
    {
        await CleanDb(_fixture);
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await new PermissionRepository(ctx)
            .ExistsByKeyAsync(PermissionKey.Create("nope.missing")));
    }
}
