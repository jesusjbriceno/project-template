using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions.Persistence;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Repositories;

namespace Project.IntegrationTests.Infrastructure.Data.Repositories;

/// <summary>
/// Integration tests for <see cref="BaseRepository{TEntity,TId}"/> using
/// <see cref="User"/> as the concrete entity type against real PostgreSQL via Testcontainers.
/// Each test cleans its own data at the start.
/// </summary>
[Collection("Postgres")]
public sealed class BaseRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public BaseRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task CleanDatabaseAsync()
    {
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.UserRoles.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.RolePermissions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Users.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.RefreshTokens.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.MenuItems.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Permissions.IgnoreQueryFilters().ExecuteDeleteAsync();
        await context.Roles.IgnoreQueryFilters().ExecuteDeleteAsync();
    }

    private ApplicationDbContext CreateContext()
    {
        var scope = _fixture.CreateServiceProvider().CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    private static User CreateTestUser(string email, string passwordHash, SystemClock clock) =>
        User.Create(Email.Create(email), passwordHash, "test-creator", clock);

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var user = CreateTestUser("getbyid-exists@test.com", "hash_test", clock);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_IncludeDeleted_True_ReturnsSoftDeletedEntity()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var user = CreateTestUser("include-deleted-true@test.com", "hash_test", clock);
        user.Delete(activeSuperadmins: Array.Empty<User>(), deletedBy: "test-deleter", clock: clock);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetByIdAsync(user.Id, includeDeleted: true);

        Assert.NotNull(result);
        Assert.True(result!.IsDeleted);
    }

    [Fact]
    public async Task GetByIdAsync_IncludeDeleted_False_HidesSoftDeletedEntity()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var user = CreateTestUser("include-deleted-false@test.com", "hash_test", clock);
        user.Delete(activeSuperadmins: Array.Empty<User>(), deletedBy: "test-deleter", clock: clock);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetByIdAsync(user.Id, includeDeleted: false);

        Assert.Null(result);
    }

    // ── AddAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_PersistsEntity()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var user = CreateTestUser("addasync-persist@test.com", "hash_add_test", clock);

        var repo = new UserRepository(context);
        await repo.AddAsync(user);

        context.ChangeTracker.Clear();
        var retrieved = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(user.Email, retrieved!.Email);
    }

    // ── GetPagedAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPageAndMetadata()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        for (int i = 1; i <= 5; i++)
        {
            context.Users.Add(CreateTestUser($"paged-{i}@test.com", $"hash_paged_{i}", clock));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var page1 = await repo.GetPagedAsync(new PageRequest(Page: 1, PageSize: 2));

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.True(page1.HasNextPage);
        Assert.False(page1.HasPreviousPage);
    }

    [Fact]
    public async Task GetPagedAsync_IncludeDeleted_IncludesSoftDeletedInPage()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var active = CreateTestUser("paged-active@test.com", "h1", clock);
        var deleted = CreateTestUser("paged-deleted@test.com", "h2", clock);
        deleted.Delete(activeSuperadmins: Array.Empty<User>(), deletedBy: "test", clock: clock);

        context.Users.AddRange(active, deleted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetPagedAsync(new PageRequest(Page: 1, PageSize: 10), includeDeleted: true);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    // ── Update / Delete ─────────────────────────────────────────────────────
    [Fact]
    public async Task Update_PersistsModifications_AfterSaveChangesAsync()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();
        var clock = new SystemClock();
        var user = CreateTestUser("update@test.com", "h1", clock);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        user.Deactivate("test-updater", clock, Array.Empty<User>());
        new UserRepository(context).Update(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reloaded = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(reloaded);
        Assert.False(reloaded!.IsActive);
        var del = CreateTestUser("delete@test.com", "h2", clock);
        context.Users.Add(del);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        new UserRepository(context).Delete(del);
        await context.SaveChangesAsync();
        Assert.Null(await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == del.Id));
    }
}
