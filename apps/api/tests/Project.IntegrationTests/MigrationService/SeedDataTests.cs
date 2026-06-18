using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Domain.ValueObjects;
using Project.Infrastructure.Data;
using Project.MigrationService;

namespace Project.IntegrationTests.MigrationService;

/// <summary>
/// Integration tests for the seed catalog: completeness of the 22-permission catalog,
/// Superadmin role-permission assignments, and strict re-run snapshot equality.
///
/// Uses a shared <see cref="MigrationServiceFixture"/> via the collection.
/// Each test starts from a cold state via <see cref="MigrationServiceFixture.ResetDatabaseAsync"/>
/// in <c>InitializeAsync</c>, which truncates all tables and drops
/// <c>__EFMigrationsHistory</c> so the next <c>MigrateAsync</c> applies all
/// migrations from scratch. Re-run snapshot tests deliberately re-run
/// <c>SeedAsync</c> on the warm database without re-migrating.
/// </summary>
[Collection("MigrationService")]
public sealed class SeedDataTests : IAsyncLifetime
{
    private readonly MigrationServiceFixture _fixture;
    private IServiceProvider _serviceProvider = null!;

    public SeedDataTests(MigrationServiceFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Cold-start: drop the EF migration history so MigrateAsync re-applies
        // all migrations and the test exercises the real first-time path.
        await _fixture.ResetDatabaseAsync();
        _serviceProvider = _fixture.CreateServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable d)
            d.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Applies migrations and runs seed, returning the scope for assertions.
    /// Caller must dispose the scope.
    /// </summary>
    private async Task<IServiceScope> RunMigrateAndSeedAsync()
    {
        var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepo = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        await dbContext.Database.MigrateAsync();
        await SeedData.SeedAsync(
            dbContext, roleRepo, permRepo, userRepo, hasher,
            "admin@test.local", "StrongP@ssw0rd123!",
            clock, CancellationToken.None);

        return scope;
    }

    // ── Catalog completeness ─────────────────────────────────────

    [Fact]
    public async Task Seed_Catalog_HasAll22Permissions()
    {
        using var scope = await RunMigrateAndSeedAsync();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var permissions = await dbContext.Permissions
            .OrderBy(p => p.Key)
            .ToListAsync();

        Assert.Equal(22, permissions.Count);

        var expectedKeys = PermissionCatalog.All.Select(e => e.Key.Value).OrderBy(k => k).ToList();
        var actualKeys = permissions.Select(p => p.Key.Value).OrderBy(k => k).ToList();

        Assert.Equal(expectedKeys, actualKeys);
    }

    [Fact]
    public async Task Seed_SuperadminRole_HasAll22Permissions()
    {
        using var scope = await RunMigrateAndSeedAsync();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var superadminRole = await dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == "Superadmin");

        Assert.NotNull(superadminRole);
        Assert.Equal(22, superadminRole!.RolePermissions.Count);
    }

    [Fact]
    public async Task Seed_SuperadminUser_HasSuperadminRoleAssigned()
    {
        using var scope = await RunMigrateAndSeedAsync();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var superadminRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == "Superadmin");
        Assert.NotNull(superadminRole);

        var email = Email.Create("admin@test.local");
        var user = await dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email);

        Assert.NotNull(user);
        Assert.Single(user!.UserRoles, ur => ur.RoleId == superadminRole!.Id);
    }

    [Fact]
    public async Task Seed_UserRole_HasZeroPermissions()
    {
        using var scope = await RunMigrateAndSeedAsync();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userRole = await dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == "User");

        Assert.NotNull(userRole);
        Assert.Empty(userRole!.RolePermissions);
    }

    // ── Strict re-run snapshot equality ──────────────────────────

    [Fact]
    public async Task Seed_Rerun_PreservesSuperadminPasswordHashAndSecurityStamp()
    {
        // Arrange — first run
        using var scope1 = await RunMigrateAndSeedAsync();
        var db1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var email = Email.Create("admin@test.local");
        var user1 = await db1.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user1);

        var passwordHash1 = user1!.PasswordHash;
        var securityStamp1 = user1.SecurityStamp;

        scope1.Dispose();

        // Act — second run (fresh scope, same database)
        using var scope2 = _serviceProvider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleRepo2 = scope2.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepo2 = scope2.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepo2 = scope2.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher2 = scope2.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock2 = scope2.ServiceProvider.GetRequiredService<IClock>();

        await SeedData.SeedAsync(
            db2, roleRepo2, permRepo2, userRepo2, hasher2,
            "admin@test.local", "StrongP@ssw0rd123!",
            clock2, CancellationToken.None);

        var user2 = await db2.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user2);

        // Assert — exact byte equality on immutable fields
        Assert.Equal(passwordHash1, user2!.PasswordHash);
        Assert.Equal(securityStamp1, user2.SecurityStamp);
    }

    [Fact]
    public async Task Seed_Rerun_PreservesRoleAndPermissionProperties()
    {
        // Arrange — first run
        using var scope1 = await RunMigrateAndSeedAsync();
        var db1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var superadminRole1 = await db1.Roles
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Name == "Superadmin");
        Assert.NotNull(superadminRole1);

        var userRole1 = await db1.Roles
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Name == "User");
        Assert.NotNull(userRole1);

        var isSystemSuperadmin1 = superadminRole1!.IsSystem;
        var isSystemUser1 = userRole1!.IsSystem;

        var firstPermission1 = await db1.Permissions
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Key == PermissionKey.Create("users.read"));
        Assert.NotNull(firstPermission1);
        var desc1 = firstPermission1!.Description;
        var cat1 = firstPermission1.Category;

        scope1.Dispose();

        // Act — second run (fresh scope, same database)
        using var scope2 = _serviceProvider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleRepo2 = scope2.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepo2 = scope2.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepo2 = scope2.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher2 = scope2.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock2 = scope2.ServiceProvider.GetRequiredService<IClock>();

        await SeedData.SeedAsync(
            db2, roleRepo2, permRepo2, userRepo2, hasher2,
            "admin@test.local", "StrongP@ssw0rd123!",
            clock2, CancellationToken.None);

        // Assert — role properties unchanged
        var superadminRole2 = await db2.Roles
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Name == "Superadmin");
        Assert.NotNull(superadminRole2);
        Assert.Equal(isSystemSuperadmin1, superadminRole2!.IsSystem);

        var userRole2 = await db2.Roles
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Name == "User");
        Assert.NotNull(userRole2);
        Assert.Equal(isSystemUser1, userRole2!.IsSystem);

        // Assert — permission properties unchanged
        var firstPermission2 = await db2.Permissions
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Key == PermissionKey.Create("users.read"));
        Assert.NotNull(firstPermission2);
        Assert.Equal(desc1, firstPermission2!.Description);
        Assert.Equal(cat1, firstPermission2.Category);
    }
}
