extern alias ApiControllers;

using Microsoft.AspNetCore.Mvc.Testing;
using ApiProgram = ApiControllers::Program;

namespace Project.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{ApiProgram}"/> for health endpoint tests.
/// Sets required configuration as process-level environment variables before the
/// factory is created so <see cref="WebApplication.CreateBuilder(string[])"/> picks
/// them up during startup. The /health endpoint does not exercise persistence or JWT,
/// but <see cref="Program.cs"/> requires both to be present at startup.
///
/// Previous values are restored on disposal to avoid leaking state across tests.
/// </summary>
public sealed class HealthWebApplicationFactory : WebApplicationFactory<ApiProgram>
{
    private const string DummyConnectionString = "Host=localhost;Database=health_dummy;Username=dummy;Password=dummy";
    private const string TestJwtSecret = "a-very-long-secret-key-that-is-at-least-32-bytes-long!!";

    private readonly string? _previousConnectionString;
    private readonly string? _previousJwtSecret;
    private readonly string? _previousJwtIssuer;
    private readonly string? _previousJwtAudience;

    public HealthWebApplicationFactory()
    {
        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        _previousJwtSecret = Environment.GetEnvironmentVariable("Jwt__Secret");
        _previousJwtIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer");
        _previousJwtAudience = Environment.GetEnvironmentVariable("Jwt__Audience");

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", DummyConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "test-issuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "test-audience");
    }

    protected override void Dispose(bool disposing)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _previousConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", _previousJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", _previousJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", _previousJwtAudience);
        base.Dispose(disposing);
    }
}
