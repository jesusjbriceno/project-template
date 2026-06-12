// MigrationService — Phase 1 scaffold only.
// EF Core migrations and superadmin seeding are NOT YET implemented.
// This service exits immediately so the API can start without blocking.
// Real migration logic will be wired in a future phase via:
//   builder.Services.AddDbContext<ApplicationDbContext>(...)
//   builder.Services.AddHostedService<MigrationWorker>();
//   host.Run();

var builder = Host.CreateApplicationBuilder(args);

// Future: builder.Services.AddDbContext<ApplicationDbContext>(...)
// Future: builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();

// Future: host.Run(); — migration worker will apply migrations and seed, then stop.

var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("MigrationService");
logger.LogInformation("MigrationService scaffold running — no EF Core migrations or seeding implemented yet. Exiting successfully.");
Console.WriteLine("MigrationService: scaffold complete — no migrations applied.");
