using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Project.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core CLI tools (migrations, script generation).
/// Not used at runtime — the DI container (DependencyInjection.AddInfrastructure)
/// configures ApplicationDbContext with the actual connection string.
///
/// The connection string resolution order:
/// 1. <c>PROJECT_TEMPLATE_DESIGNTIME_CONNECTION</c> environment variable
/// 2. Dummy localhost fallback (syntactically valid for CLI model inspection only)
///
/// A real database connection is NOT required during migration generation —
/// EF CLI only inspects the model and existing migrations.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PROJECT_TEMPLATE_DESIGNTIME_CONNECTION")
            ?? "Host=localhost;Database=project_template_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
