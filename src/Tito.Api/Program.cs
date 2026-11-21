using Microsoft.EntityFrameworkCore;
using Tito.Infrastructure.Persistence;

// ---- 1. Register services (dependency injection container) ----
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();                          // OpenTelemetry, health checks (from Tito.ServiceDefaults)
builder.AddSqlServerDbContext<TitoDbContext>("tito");  // reads ConnectionStrings:tito, adds retries + tracing

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// ---- 2. Development-only setup ----
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                  // serves /openapi/v1.json

    // Apply pending migrations at startup - convenient locally.
    // In Azure, the CI/CD pipeline applies them instead (Phase 7).
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TitoDbContext>();
    await db.Database.MigrateAsync();
}

// ---- 3. HTTP request pipeline (middleware runs in this order) ----
app.UseHttpsRedirection();
app.MapControllers();
app.MapDefaultEndpoints();                             // /health and /alive

// Temporary Phase 2 check: proves the API can query SQL Server. Removed in Phase 3.
app.MapGet("/api/db-check", async (TitoDbContext db) => new
{
    employees = await db.Employees.CountAsync(),
    attendanceRecords = await db.AttendanceRecords.CountAsync(),
    appliedMigrations = await db.Database.GetAppliedMigrationsAsync(),
});

app.Run();
