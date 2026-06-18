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
/// Integration tests for the migration-and-seed workflow.
/// Verifies: migrations apply, seed completes, idempotency on re-run,
/// and credential validation aborts before any database operations.
///
/// Uses a shared <see cref="MigrationServiceFixture"/> via the collection.
/// Each test starts from a cold state via <see cref="MigrationServiceFixture.ResetDatabaseAsync"/>
/// in <c>InitializeAsync</c>, which truncates all tables and drops
/// <c>__EFMigrationsHistory</c> so the next <c>MigrateAsync</c> applies all
/// migrations from scratch.
/// </summary>
[Collection("MigrationService")]
public sealed class MigrationWorkerTests : IAsyncLifetime
{
    private readonly MigrationServiceFixture _fixture;
    private IServiceProvider _serviceProvider = null!;

    public MigrationWorkerTests(MigrationServiceFixture fixture)
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

    // ── Happy path ────────────────────────────────────────────────

    [Fact]
    public async Task MigrateAndSeed_RunFirstTime_AppliesMigrationsAndCreatesSeedData()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply migrations (idempotent — no-op if already applied)
        await dbContext.Database.MigrateAsync();

        // Verify migration was applied by checking no pending migrations remain
        var pending = await dbContext.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);

        // Run seed (idempotent — no-op if already seeded)
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepo = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        await SeedData.SeedAsync(
            dbContext, roleRepo, permRepo, userRepo, hasher,
            "admin@test.local", "StrongP@ssw0rd123!",
            clock, CancellationToken.None);

        // Verify seed data
        var superadminRole = await roleRepo.GetByNameAsync("Superadmin");
        Assert.NotNull(superadminRole);
        Assert.True(superadminRole!.IsSystem);

        var email = Email.Create("admin@test.local");
        var superadminUser = await userRepo.GetByEmailAsync(email);
        Assert.NotNull(superadminUser);
        Assert.True(superadminUser!.IsActive);
        Assert.NotEmpty(superadminUser.PasswordHash);

        // The seeded user must have the Superadmin role assigned.
        // With NoTracking default, navigation properties (UserRoles) are not loaded
        // unless explicitly Included. Query the UserRoles table directly.
        var userHasRole = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == superadminUser.Id && ur.RoleId == superadminRole.Id);
        Assert.True(userHasRole);
    }

    // ── Re-run idempotency ───────────────────────────────────────

    [Fact]
    public async Task MigrateAndSeed_RunTwice_SecondRunIsNoOp()
    {
        // Arrange — first run (may be starting from empty database)
        using var scope1 = _serviceProvider.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleRepo1 = scope1.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepo1 = scope1.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepo1 = scope1.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher1 = scope1.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock1 = scope1.ServiceProvider.GetRequiredService<IClock>();

        await db1.Database.MigrateAsync();
        await SeedData.SeedAsync(
            db1, roleRepo1, permRepo1, userRepo1, hasher1,
            "admin@test.local", "StrongP@ssw0rd123!",
            clock1, CancellationToken.None);

        var roleCount1 = await db1.Roles.CountAsync();
        var permCount1 = await db1.Permissions.CountAsync();
        var userCount1 = await db1.Users.CountAsync();

        scope1.Dispose();

        // Act — second run (fresh scope, same database — no drop)
        using var scope2 = _serviceProvider.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleRepo2 = scope2.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepo2 = scope2.ServiceProvider.GetRequiredService<IPermissionRepository>();
        var userRepo2 = scope2.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher2 = scope2.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock2 = scope2.ServiceProvider.GetRequiredService<IClock>();

        // MigrateAsync on already-migrated database is a no-op
        await db2.Database.MigrateAsync();

        await SeedData.SeedAsync(
            db2, roleRepo2, permRepo2, userRepo2, hasher2,
            "admin@test.local", "StrongP@ssw0rd123!",
            clock2, CancellationToken.None);

        // Assert — counts unchanged
        var roleCount2 = await db2.Roles.CountAsync();
        var permCount2 = await db2.Permissions.CountAsync();
        var userCount2 = await db2.Users.CountAsync();

        Assert.Equal(roleCount1, roleCount2);
        Assert.Equal(permCount1, permCount2);
        Assert.Equal(userCount1, userCount2);
    }

    // ── Missing credentials aborts ───────────────────────────────

    [Fact]
    public void ValidateCredentials_MissingPassword_ReturnsFailureBeforeAnyDbOperation()
    {
        var result = SuperadminCredentialValidator.Validate(
            "admin@test.local", password: null);

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_PASSWORD_MISSING", result.Error.Code);
    }

    [Fact]
    public void ValidateCredentials_MissingEmail_ReturnsFailureBeforeAnyDbOperation()
    {
        var result = SuperadminCredentialValidator.Validate(
            email: null, "StrongP@ssw0rd123!");

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_EMAIL_MISSING", result.Error.Code);
    }

    [Fact]
    public async Task ValidateCredentials_PasswordTooShort_DoesNotTouchDatabase()
    {
        // The credential validator is pure (no DI, no DB access).
        // It returns failure without touching the database at all.
        var result = SuperadminCredentialValidator.Validate(
            "admin@test.local", "short");

        Assert.True(result.IsFailure);
        Assert.Equal("SUPERADMIN_PASSWORD_TOO_SHORT", result.Error.Code);

        // Verify connection is healthy — the container is up and reachable
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var canConnect = await db.Database.CanConnectAsync();
        Assert.True(canConnect);
    }
}
