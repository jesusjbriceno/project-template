extern alias ApiControllers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Infrastructure.Security;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Auth;

/// <summary>
/// Custom <see cref="WebApplicationFactory{ApiProgram}"/> for auth integration tests.
/// Overrides JWT configuration and database connection string at startup,
/// registers a test-only protected endpoint controller, and seeds a test user
/// so login tests can authenticate successfully.
/// </summary>
public sealed class AuthWebApplicationFactory : WebApplicationFactory<ApiProgram>
{
    /// <summary>
    /// Non-secret test fixture constant — used ONLY for integration tests.
    /// Never used in production; production JWT secrets are read from environment variables.
    /// Must be ≥32 bytes to satisfy <c>[MinLength(32)]</c> on <see cref="JwtOptions.Secret"/>.
    /// </summary>
    private const string TestJwtSecret = "a-very-long-secret-key-that-is-at-least-32-bytes-long!!";

    private readonly string _connectionString;

    public AuthWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
            });
        });

        // Register the test-only protected endpoint controller so AuthMiddlewareTests
        // can verify JWT Bearer middleware without leaking /auth/me to production.
        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(TestAuthController).Assembly);
        });

        // Use test environment so Secure cookie policy is SameAsRequest
        builder.UseEnvironment("Development");
    }
}
