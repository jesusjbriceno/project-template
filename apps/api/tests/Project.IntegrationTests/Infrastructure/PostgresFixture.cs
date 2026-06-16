using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.DependencyInjection;
using Project.Infrastructure;
using Project.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Project.IntegrationTests.Infrastructure;

/// <summary>
/// Shared PostgreSQL 17 container fixture for infrastructure integration tests.
/// Applies EF Core schema and truncates tables between test classes.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(
                ["pg_isready", "-U", "testuser", "-d", "testdb"]))
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply EF Core schema against the container
        var services = new ServiceCollection();
        services.AddInfrastructure(ConnectionString);
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Drop and recreate to ensure schema matches the current model
        // (entity configurations are applied via OnModelCreating).
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a fresh IServiceProvider with Infrastructure services wired to the test container.
    /// </summary>
    public IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(ConnectionString);
        return services.BuildServiceProvider();
    }
}
