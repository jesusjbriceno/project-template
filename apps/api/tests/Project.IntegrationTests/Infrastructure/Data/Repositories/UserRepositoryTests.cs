using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Repositories;

namespace Project.IntegrationTests.Infrastructure.Data.Repositories;

/// <summary>
/// Integration tests for <see cref="UserRepository"/> aggregate-specific queries
/// against real PostgreSQL via Testcontainers.
/// Each test cleans its own data at the start.
/// </summary>
[Collection("Postgres")]
public sealed class UserRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public UserRepositoryTests(PostgresFixture fixture)
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

    // ── GetByEmailAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_MatchingEmail_ReturnsUser()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var user = User.Create(Email.Create("find-by-email@test.com"), "hash_email", "test-creator", clock);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetByEmailAsync(Email.Create("find-by-email@test.com"));

        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal("find-by-email@test.com", result.Email.Value);
    }

    [Fact]
    public async Task GetByEmailAsync_NonMatchingEmail_ReturnsNull()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var repo = new UserRepository(context);
        var result = await repo.GetByEmailAsync(Email.Create("nonexistent@test.com"));

        Assert.Null(result);
    }

    // ── ExistsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ExistingUser_ReturnsTrue()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var user = User.Create(Email.Create("exists-true@test.com"), "hash_exists", "test-creator", clock);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.ExistsAsync(user.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingUser_ReturnsFalse()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var repo = new UserRepository(context);
        var result = await repo.ExistsAsync(UserId.New());

        Assert.False(result);
    }

    // ── GetActiveSuperadminsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetActiveSuperadminsAsync_ReturnsActiveUsersWithSuperadminRole()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        context.Roles.Add(superadminRole);

        var superadminUser = User.Create(Email.Create("superadmin@test.com"), "hash_sa", "system", clock);
        context.Users.Add(superadminUser);
        await context.SaveChangesAsync();

        var userRole = UserRole.Assign(superadminUser.Id, superadminRole.Id, "system", clock);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetActiveSuperadminsAsync();

        Assert.NotEmpty(result);
        var found = Assert.Single(result);
        Assert.Equal(superadminUser.Id, found.Id);
    }

    [Fact]
    public async Task GetActiveSuperadminsAsync_InactiveSuperadmin_NotReturned()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        context.Roles.Add(superadminRole);

        var inactiveUser = User.Create(Email.Create("inactive-sa@test.com"), "hash_isa", "system", clock);
        context.Users.Add(inactiveUser);
        await context.SaveChangesAsync();

        var userRole = UserRole.Assign(inactiveUser.Id, superadminRole.Id, "system", clock);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        inactiveUser.Deactivate("system", clock, activeSuperadmins: Array.Empty<User>());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new UserRepository(context);
        var result = await repo.GetActiveSuperadminsAsync();

        Assert.Empty(result);
    }
}
