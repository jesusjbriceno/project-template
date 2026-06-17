using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Infrastructure.Data;
using Project.Infrastructure.Data.Interceptors;
using Project.Infrastructure.Data.Repositories;
using Project.Infrastructure.Security;

namespace Project.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services with DI.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services: DbContext, interceptors, clock, security scaffold.
    /// The connection string is passed directly rather than via IConfiguration to keep
    /// Infrastructure decoupled from ASP.NET configuration sources.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        // Clock abstraction — bound to real system clock
        services.AddSingleton<IClock, SystemClock>();

        // Audit timestamp interceptor (depends on IClock via scoped resolution)
        services.AddSingleton<AuditTimestampInterceptor>();

        // Security scaffold — real IUserSession wired later in ef-core-security slice
        services.AddScoped<IUserSession, NullUserSession>();

        // EF Core DbContext with PostgreSQL
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // NoTracking by default for all queries (read-heavy workloads)
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // Register audit interceptor
            var interceptor = sp.GetRequiredService<AuditTimestampInterceptor>();
            options.AddInterceptors(interceptor);
        });

        // Repositories — scoped to match DbContext lifetime
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();

        return services;
    }
}
