using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Application.Auth;
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
    /// Registers all Infrastructure services: DbContext, interceptors, clock,
    /// repositories, password hasher, user session, and token service adapter.
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

        // HttpContext accessor — required by UserSession to extract claims from the JWT principal
        services.AddHttpContextAccessor();

        // User session — real implementation replacing NullUserSession scaffold
        services.AddScoped<IUserSession, UserSession>();

        // Token service adapter — thin wrapper over IRefreshTokenRepository.RevokeFamilyAsync
        services.AddScoped<ITokenService, TokenService>();

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

        // Password hasher — stateless, singleton lifetime
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // Repositories — scoped to match DbContext lifetime
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();

        return services;
    }

    /// <summary>
    /// Registers JWT authentication services: binds and validates <see cref="JwtOptions"/>,
    /// registers <see cref="IJwtTokenService"/>, and configures the JWT Bearer authentication
    /// handler with HS256 validation parameters.
    /// </summary>
    /// <remarks>
    /// JWT configuration is read from the "Jwt" section of application configuration.
    /// Options are validated on startup via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
    /// </remarks>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind and validate JwtOptions from configuration
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Application-owned token lifetime options — mapped from Infrastructure JWT options
        // so Application handlers do not depend on Infrastructure configuration types.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            return new AuthTokenOptions { RefreshTokenDays = options.RefreshTokenDays };
        });

        // IJwtTokenService — singleton because it holds the signing key (derived from JwtOptions)
        // and JwtSecurityTokenHandler is thread-safe
        services.AddSingleton<IJwtTokenService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            var clock = sp.GetRequiredService<IClock>();
            return new JwtTokenService(options, clock);
        });

        // JWT Bearer authentication — validates access tokens on every authenticated request
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Resolve JwtOptions at configuration time (not per-request)
                var jwtSection = configuration.GetSection("Jwt");
                var secret = jwtSection["Secret"] ?? string.Empty;
                var issuer = jwtSection["Issuer"] ?? string.Empty;
                var audience = jwtSection["Audience"] ?? string.Empty;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };

                options.MapInboundClaims = false;
            });

        return services;
    }
}
