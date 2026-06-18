using Project.Infrastructure;
using Project.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

// ── Database connection ──

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. " +
        "Set ConnectionStrings:DefaultConnection in appsettings.json or as an environment variable.");

// ── Infrastructure (DbContext, repositories, password hasher, clock) ──

builder.Services.AddInfrastructure(connectionString);

// ── Migration worker ──

builder.Services.AddHostedService<MigrationWorker>();

// ── Run ──

var host = builder.Build();
await host.RunAsync();
