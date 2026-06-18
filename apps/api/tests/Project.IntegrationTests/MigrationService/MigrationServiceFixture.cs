using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Project.Infrastructure;
using Project.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Project.IntegrationTests.MigrationService;

/// <summary>
/// Dedicated PostgreSQL 17 container fixture for MigrationService integration tests.
/// Unlike <see cref="Infrastructure.PostgresFixture"/>, this fixture does NOT
/// call EnsureCreated so that tests exercise <c>MigrateAsync</c> (EF migration history)
/// and seed data from a completely empty database.
///
/// Isolation strategy: tests call <see cref="ResetDatabaseAsync"/> in
/// <c>InitializeAsync</c> to drop and recreate the test database before every test,
/// guaranteeing cold-start coverage of the migration/seed pipeline.
/// </summary>
public sealed class MigrationServiceFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public MigrationServiceFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("testmigrationdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                ["pg_isready", "-U", "testuser", "-d", "testmigrationdb"]))
            .WithCleanUp(true)
            .Build();

        DatabaseName = "testmigrationdb";
    }

    public string DatabaseName { get; }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Database is completely empty — schema is created by MigrateAsync in tests.
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh <see cref="IServiceProvider"/> with Infrastructure services
    /// wired to the test container.
    /// </summary>
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(ConnectionString);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Wipes the migration/seed data so each test starts from a clean cold-start
    /// state. Uses raw SQL TRUNCATE to avoid the migration history cache and to
    /// stay independent of EF Core model snapshots. The PostgreSQL
    /// <c>__EFMigrationsHistory</c> table is also removed so the next
    /// <c>MigrateAsync</c> call re-applies all migrations from scratch.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        // Drop the test database via a maintenance connection (against the
        // default `postgres` database) and then recreate it empty. This is the
        // only reliable cold-start because:
        //   1. The schema only exists after the first test runs MigrateAsync,
        //      so TRUNCATE on the very first call would fail.
        //   2. EnsureDeletedAsync on the test database invalidates the
        //      connection pool for the same connection string, requiring the
        //      recreate to use a different connection target.
        //   3. We want MigrateAsync to be exercised on every test (its true
        //      first-time path) — not skipped because the schema already
        //      exists from a previous test.
        const string maintenanceDatabase = "postgres";
        var connectionString = ConnectionString;

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = maintenanceDatabase
        };
        var maintenanceConnection = builder.ConnectionString;

        await using (var maintenance = new Npgsql.NpgsqlConnection(maintenanceConnection))
        {
            await maintenance.OpenAsync();

            // Terminate any active sessions on the test database, then drop it.
            // DROP DATABASE WITH (FORCE) is available on PostgreSQL 13+.
            await using (var terminate = maintenance.CreateCommand())
            {
                terminate.CommandText = $@"
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = '{DatabaseName}'
                      AND pid <> pg_backend_pid();
                ";
                await terminate.ExecuteNonQueryAsync();
            }

            await using (var drop = maintenance.CreateCommand())
            {
                drop.CommandText = $@"DROP DATABASE IF EXISTS ""{DatabaseName}"";";
                await drop.ExecuteNonQueryAsync();
            }

            await using (var create = maintenance.CreateCommand())
            {
                create.CommandText = $@"CREATE DATABASE ""{DatabaseName}"";";
                await create.ExecuteNonQueryAsync();
            }
        }

        // The Npgsql connection pool may have open connections to the dropped
        // database. Clear it so the next test gets a fresh connection to the
        // recreated database.
        NpgsqlConnection.ClearAllPools();
    }
}
