using FluentValidation;
using Project.Api.Controllers.Controllers;
using Project.Api.Controllers.Middleware;
using Project.Application.Auth;
using Project.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────
builder.Services.AddHealthChecks();

// Controllers
builder.Services.AddControllers();

// Enum serialization — values appear as strings in JSON responses.
// Controllers path (MVC).
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Minimal APIs path (health checks, etc.).
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// RFC 7807 ProblemDetails support — enables consistent error responses.
builder.Services.AddProblemDetails();

// Global exception handler — catches unhandled exceptions and returns safe 500s.
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Infrastructure (EF Core, repositories, password hasher, user session, clock)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddInfrastructure(connectionString);

// JWT authentication (JwtOptions binding, IJwtTokenService, JwtBearer handler)
builder.Services.AddJwtAuthentication(builder.Configuration);

// Authorization services (policy evaluation, [Authorize] attribute support)
builder.Services.AddAuthorization();

// ── Auth handlers (called directly by controller, no MediatR) ──
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddScoped<RefreshTokenCommandHandler>();
builder.Services.AddScoped<LogoutCommandHandler>();

// ── Auth validators (FluentValidation) ──
builder.Services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
builder.Services.AddScoped<IValidator<LogoutCommand>, LogoutCommandValidator>();

// ── Auth controller ──
builder.Services.AddScoped<AuthController>();

// OpenAPI document generation — Development only.
// Uses .NET 10 built-in support (no Swashbuckle).
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────
// Exception/status handling BEFORE auth so framework-generated
// 404/405 and pipeline exceptions normalize consistently.
app.UseExceptionHandler();
app.UseStatusCodePages(FrameworkStatusCodePages.WriteAsync);

app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────
app.MapHealthChecks("/health");

// OpenAPI document endpoint — Development only.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// ── Run ──────────────────────────────────────────────────
app.Run();

// Make the Program class accessible to integration tests via WebApplicationFactory.
/// <summary>
/// Composition root for the API host. Configures services, middleware pipeline,
/// and endpoint mappings. Partial class to allow test access via
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program { }
