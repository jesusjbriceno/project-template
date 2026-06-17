using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Project.Infrastructure.Data;

namespace Project.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for ApplicationDbContext resolution, schema creation,
/// NoTracking default, and SplitQuery behavior against real PostgreSQL via Testcontainers.
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

    /// <summary>
    /// Genuinely verifies that QuerySplittingBehavior.SplitQuery is configured
    /// in the DbContext options by inspecting the relational options extension metadata.
    ///
    /// The Npgsql-specific extension (NpgsqlOptionsExtension) inherits from
    /// RelationalOptionsExtension, which carries the QuerySplittingBehavior property
    /// set by DependencyInjection.AddInfrastructure via
    /// <c>npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)</c>.
    ///
    /// Full multi-Include runtime SQL proof is deferred because the domain model
    /// intentionally uses navigationless junction entities (UserRole/RolePermission
    /// carry FK IDs only — no object references to Role/Permission), making
    /// multi-level .Include()/.ThenInclude() chains impossible without artificial
    /// model changes that would violate the DDD/Clean Architecture design.
    /// </summary>
    [Fact]
    public void SplitQuery_Is_Configured_In_DbContext_Options()
    {
        using var scope = _fixture.CreateServiceProvider().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Access the DbContextOptions via the EF Core internal service provider.
        // DbContext implements IInfrastructure<IServiceProvider>; GetService<T>()
        // resolves internal services including IDbContextOptions.
        var dbContextOptions = (DbContextOptions)context
            .GetService<IDbContextOptions>();

        // The Npgsql provider replaces the base RelationalOptionsExtension with
        // its own NpgsqlOptionsExtension which inherits from RelationalOptionsExtension.
        // The QuerySplittingBehavior was set by DependencyInjection.AddInfrastructure
        // via npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery).
        var relationalExtension = dbContextOptions.Extensions
            .OfType<RelationalOptionsExtension>()
            .FirstOrDefault();

        Assert.NotNull(relationalExtension);
        Assert.Equal(
            QuerySplittingBehavior.SplitQuery,
            relationalExtension.QuerySplittingBehavior);
    }
}
