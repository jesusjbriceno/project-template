using FluentValidation;
using Project.Api.Controllers.Controllers;
using Project.Application.Auth;
using Project.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────
builder.Services.AddHealthChecks();

// Controllers
builder.Services.AddControllers();

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

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapControllers();

// ── Run ──────────────────────────────────────────────────
app.Run();

// Make the Program class accessible to integration tests via WebApplicationFactory.
public partial class Program { }
