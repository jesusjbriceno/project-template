using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Repositories;

namespace Project.IntegrationTests.Infrastructure.Data.Repositories;

/// <summary>
/// Integration tests for <see cref="RoleRepository"/> aggregate-specific queries
/// against real PostgreSQL via Testcontainers.
/// Each test cleans its own data at the start.
/// </summary>
[Collection("Postgres")]
public sealed class RoleRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public RoleRepositoryTests(PostgresFixture fixture)
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

    // ── ExistsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_ExistingRole_ReturnsTrue()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var role = Role.Create("TestRole", isSystem: false, "test-creator", clock);
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new RoleRepository(context);
        var result = await repo.ExistsAsync(role.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingRole_ReturnsFalse()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var repo = new RoleRepository(context);
        var result = await repo.ExistsAsync(RoleId.New());

        Assert.False(result);
    }

    // ── GetSystemRolesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetSystemRolesAsync_ReturnsOnlySystemRoles()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var systemRole = Role.Create("superadmin", isSystem: true, "system", clock);
        var customRole = Role.Create("CustomRole", isSystem: false, "test-creator", clock);
        context.Roles.AddRange(systemRole, customRole);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new RoleRepository(context);
        var result = await repo.GetSystemRolesAsync();

        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.True(r.IsSystem));
        Assert.Contains(result, r => r.Name == "superadmin");
        Assert.DoesNotContain(result, r => r.Name == "CustomRole");
    }

    [Fact]
    public async Task GetSystemRolesAsync_NoSystemRoles_ReturnsEmpty()
    {
        await CleanDatabaseAsync();
        using var context = CreateContext();

        var clock = new SystemClock();
        var customRole = Role.Create("OnlyCustom", isSystem: false, "test-creator", clock);
        context.Roles.Add(customRole);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new RoleRepository(context);
        var result = await repo.GetSystemRolesAsync();

        Assert.Empty(result);
    }
}
