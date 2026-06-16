using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Repositories;

namespace Project.IntegrationTests.Infrastructure.Data.Repositories;

[Collection("Postgres")]
public sealed class MenuItemRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;
    public MenuItemRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    private static async Task CleanDb(PostgresFixture f)
    {
        using var scope = f.CreateServiceProvider().CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .MenuItems.IgnoreQueryFilters().ExecuteDeleteAsync();
    }
    private async Task<(ApplicationDbContext, MenuItemRepository)> Setup()
    {
        var scope = _fixture.CreateServiceProvider().CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (ctx, new MenuItemRepository(ctx));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNonDeletedItems()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var v = CreateItem("V", null, "/v", null, 1, clock);
        var d = CreateItem("D", null, "/d", null, 2, clock);
        d.Delete("del", clock);
        ctx.MenuItems.AddRange(v, d);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var result = await repo.GetAllAsync();
        Assert.Single(result);
        Assert.Equal(v.Id, result.First().Id);
    }

    [Fact]
    public async Task GetChildrenAsync_ReturnsDirectChildren()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var p = CreateItem("Parent", null, "/p", null, 1, clock);
        ctx.MenuItems.Add(p);
        await ctx.SaveChangesAsync();

        var c1 = CreateItem("C1", null, "/c1", p.Id, 1, clock);
        var c2 = CreateItem("C2", null, "/c2", p.Id, 2, clock);
        ctx.MenuItems.AddRange(c1, c2);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var result = await repo.GetChildrenAsync(p.Id);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetChildrenAsync_NoChildren_ReturnsEmpty()
    {
        await CleanDb(_fixture);
        var (ctx, repo) = await Setup();
        var clock = new SystemClock();
        var p = CreateItem("Leaf", null, "/l", null, 1, clock);
        ctx.MenuItems.Add(p);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        Assert.Empty(await repo.GetChildrenAsync(p.Id));
    }

    private static MenuItem CreateItem(string label, string? icon, string? route,
        MenuItemId? parentId, int sort, IClock clock)
        => MenuItem.Create(label, icon, route, parentId, sort, null, null, true, "test", clock);
}
