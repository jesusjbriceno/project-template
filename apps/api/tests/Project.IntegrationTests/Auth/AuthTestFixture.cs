extern alias ApiControllers;

using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Infrastructure;
using Project.Infrastructure.Data;
using Testcontainers.PostgreSql;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Shared fixture for auth integration tests.
/// Starts a PostgreSQL 17 container via Testcontainers, applies schema,
/// seeds a test user, and creates an <see cref="HttpClient"/> for the
/// <see cref="AuthWebApplicationFactory"/>.
///
/// Clients are created with <c>HandleCookies = false</c> so tests fully control
/// cookie transmission without automatic cookie-jar interference.
/// This is essential for rotation and reuse tests where the exact stale token
/// must be presented manually.
/// </summary>
public sealed class AuthTestFixture : IAsyncLifetime
{
    public const string TestUserEmail = "testuser@example.com";
    public const string TestUserPassword = "StrongP@ssw0rd!";

    private readonly PostgreSqlContainer _container;

    public AuthTestFixture()
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

        var connectionString = _container.GetConnectionString();

        // Apply EF Core schema against the container
        var services = new ServiceCollection();
        services.AddInfrastructure(connectionString);
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Seed a test user for login tests
        var clock = new SystemClock();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = User.Create(
            Email.Create(TestUserEmail),
            hasher.Hash(TestUserPassword),
            "system",
            clock);

        context.Set<User>().Add(user);
        await context.SaveChangesAsync();

        // Create the factory AFTER seeding
        _factory = new AuthWebApplicationFactory(connectionString);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private AuthWebApplicationFactory? _factory;

    /// <summary>
    /// Creates a fresh <see cref="HttpClient"/> against the auth-enabled API
    /// with automatic cookie handling DISABLED.
    ///
    /// Tests must manually extract <c>Set-Cookie</c> values from responses
    /// and pass the exact cookie under test via the <c>Cookie</c> request header.
    /// This avoids ambiguity between the client's cookie jar and manually-set
    /// cookies, which is critical for rotation/reuse scenarios where the
    /// test must prove the stale token is presented.
    /// </summary>
    public HttpClient CreateClient()
    {
        if (_factory is null)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
    }
}
