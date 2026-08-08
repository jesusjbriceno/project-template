extern alias ApiControllers;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests.Middleware;

/// <summary>
/// Integration tests for OpenAPI endpoint gating.
/// Verifies that the OpenAPI document endpoint is available in Development
/// but returns 404 in Production.
/// </summary>
public sealed class OpenApiGatingTests
{
    // ── Development: OpenAPI available ─────────────────────────

    /// <summary>
    /// Task 3.3: In Development, GET /openapi/v1.json returns 200 with valid OpenAPI JSON.
    /// </summary>
    [Fact]
    public async Task OpenApi_InDevelopment_Returns200WithValidDocument()
    {
        // ARRANGE
        await using var factory = new DevelopmentOpenApiFactory();
        using var client = factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/openapi/v1.json");

        // ASSERT — 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // ASSERT — body is valid JSON with OpenAPI structure
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));

        var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("openapi", out var openApiVersion));
        Assert.StartsWith("3.", openApiVersion.GetString());

        // ASSERT — document references at least one path (the API has endpoints)
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.EnumerateObject().Any());
    }

    // ── Production: OpenAPI unavailable ────────────────────────

    /// <summary>
    /// Task 3.3: In Production, GET /openapi/v1.json returns 404.
    /// </summary>
    [Fact]
    public async Task OpenApi_InProduction_Returns404()
    {
        // ARRANGE
        await using var factory = new ProductionOpenApiFactory();
        using var client = factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/openapi/v1.json");

        // ASSERT — 404 Not Found (OpenAPI endpoint not registered in Production)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Test Factories ─────────────────────────────────────────

    /// <summary>
    /// Factory that sets the environment to Development so OpenAPI is registered.
    /// </summary>
    private sealed class DevelopmentOpenApiFactory : WebApplicationFactory<ApiProgram>, IAsyncDisposable
    {
        private readonly string? _prevConnectionString;
        private readonly string? _prevJwtSecret;
        private readonly string? _prevJwtIssuer;
        private readonly string? _prevJwtAudience;

        public DevelopmentOpenApiFactory()
        {
            _prevConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            _prevJwtSecret = Environment.GetEnvironmentVariable("Jwt__Secret");
            _prevJwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
            _prevJwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience");

            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
                "Host=localhost;Database=openapi_dev_dummy;Username=dummy;Password=dummy");
            Environment.SetEnvironmentVariable("Jwt__Secret",
                "a-very-long-secret-key-that-is-at-least-32-bytes-long!!");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "test-issuer");
            Environment.SetEnvironmentVariable("Jwt__Audience", "test-audience");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }

        protected override void Dispose(bool disposing)
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _prevConnectionString);
            Environment.SetEnvironmentVariable("Jwt__Secret", _prevJwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", _prevJwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", _prevJwtAudience);
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Factory that sets the environment to Production so OpenAPI is NOT registered.
    /// </summary>
    private sealed class ProductionOpenApiFactory : WebApplicationFactory<ApiProgram>, IAsyncDisposable
    {
        private readonly string? _prevConnectionString;
        private readonly string? _prevJwtSecret;
        private readonly string? _prevJwtIssuer;
        private readonly string? _prevJwtAudience;

        public ProductionOpenApiFactory()
        {
            _prevConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            _prevJwtSecret = Environment.GetEnvironmentVariable("Jwt__Secret");
            _prevJwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
            _prevJwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience");

            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
                "Host=localhost;Database=openapi_prod_dummy;Username=dummy;Password=dummy");
            Environment.SetEnvironmentVariable("Jwt__Secret",
                "a-very-long-secret-key-that-is-at-least-32-bytes-long!!");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "test-issuer");
            Environment.SetEnvironmentVariable("Jwt__Audience", "test-audience");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
        }

        protected override void Dispose(bool disposing)
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _prevConnectionString);
            Environment.SetEnvironmentVariable("Jwt__Secret", _prevJwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", _prevJwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", _prevJwtAudience);
            base.Dispose(disposing);
        }
    }
}
