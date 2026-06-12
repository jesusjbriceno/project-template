using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────
builder.Services.AddHealthChecks();

// Future: AddOpenApi(), AddControllers(), AddDbContext<AppDbContext>(...),
//         AddScoped<IUserRepository, UserRepository>(), etc.

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────
// Future: app.UseExceptionHandler(), app.UseStatusCodePages(),
//         app.UseAuthentication(), app.UseAuthorization().

// ── Health endpoint ───────────────────────────────────────
app.MapHealthChecks("/health");

// ── Run ──────────────────────────────────────────────────
app.Run();

// Make the Program class accessible to integration tests via WebApplicationFactory.
public partial class Program { }
