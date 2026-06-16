using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Infrastructure.Data;

namespace Project.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for ApplicationDbContext resolution and schema creation
/// against a real PostgreSQL 17 container via Testcontainers.
/// </summary>
[Collection("Postgres")]
public sealed class ApplicationDbContextTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public ApplicationDbContextTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Di_Can_Resolve_ApplicationDbContext()
    {
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public async Task Can_Create_Database_And_Connect()
    {
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // EnsureCreated creates the schema; should succeed against the test container
        await context.Database.EnsureCreatedAsync();

        // Verify we can open/close the connection
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public void ApplicationDbContext_Uses_NoTracking_By_Default()
    {
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(
            QueryTrackingBehavior.NoTracking,
            context.ChangeTracker.QueryTrackingBehavior);
    }
}
