using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Infrastructure.Data;

namespace Project.MigrationService;

/// <summary>
/// BackgroundService that applies pending EF Core migrations, seeds the RBAC catalog
/// and initial superadmin account, then stops the host so Docker Compose can safely
/// start the API.
///
/// Flow: validate credentials → MigrateAsync (retry strategy) → SeedAsync → StopApplication.
/// Any fatal failure (invalid credentials, migration or seed exception) is rethrown so the
/// Generic Host terminates with a non-zero exit code, preventing the API from starting
/// against an uninitialized database.
/// </summary>
public sealed class MigrationWorker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrationWorker> _logger;

    public MigrationWorker(
        IHostApplicationLifetime hostLifetime,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<MigrationWorker> logger)
    {
        _hostLifetime = hostLifetime;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Migration service starting...");

        try
        {
            // ── 1. Validate credentials ──
            //
            // Configuration errors are catastrophic: a deployed system without
            // superadmin access is invalid. We surface them as exceptions so the
            // Generic Host terminates with a non-zero exit code, which keeps
            // Docker Compose / Kubernetes from starting dependent services
            // (e.g. the API) against an uninitialized database.

            var email = _configuration["SUPERADMIN_EMAIL"];
            var password = _configuration["SUPERADMIN_PASSWORD"];

            var credentialResult = SuperadminCredentialValidator.Validate(email, password);

            if (credentialResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Superadmin credential validation failed: {credentialResult.Error.Code} — {credentialResult.Error.Message}");
            }

            var credentials = credentialResult.Value!;

            // ── 2. Apply migrations + seed ──

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
            var permissionRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();

            await RunMigrationsAsync(dbContext, stoppingToken);
            await SeedData.SeedAsync(
                dbContext,
                roleRepository,
                permissionRepository,
                userRepository,
                passwordHasher,
                credentials.Email,
                credentials.PlaintextPassword,
                clock,
                stoppingToken);

            _logger.LogInformation("Migration service completed successfully.");

            // On the success path, stop the host so the container exits cleanly
            // with exit code 0. On the failure path we MUST NOT call this; we
            // rely on the unhandled exception propagating up to the Generic
            // Host, which terminates the process with a non-zero exit code
            // and prevents dependent services (e.g. the API) from starting
            // against an uninitialized database.
            _hostLifetime.StopApplication();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration service failed: {Error}", ex.Message);
            // Rethrow so the Generic Host terminates with a non-zero exit code.
            // Never swallow bootstrap failures.
            throw;
        }
    }

    /// <summary>
    /// Applies pending EF Core migrations inside the Npgsql retry execution strategy.
    /// Logs the count of pending migrations before/after applying.
    /// </summary>
    private async Task RunMigrationsAsync(ApplicationDbContext dbContext, CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(ct);

            if (pendingMigrations.Any())
            {
                _logger.LogInformation(
                    "Applying {Count} pending migration(s): {Migrations}",
                    pendingMigrations.Count(),
                    string.Join(", ", pendingMigrations));

                await dbContext.Database.MigrateAsync(ct);

                // Note: we log the pending count we just applied, not a re-query
                // of the applied list. Re-querying GetAppliedMigrationsAsync on
                // the same DbContext right after MigrateAsync returns a stale
                // (often empty) result because EF Core caches the migration
                // history state for this context instance.
                _logger.LogInformation(
                    "Migrations applied successfully. {Count} migration(s) applied in this run.",
                    pendingMigrations.Count());
            }
            else
            {
                _logger.LogInformation("No pending migrations. Database is up to date.");
            }
        });
    }
}
