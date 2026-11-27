using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Tito.Api.Configuration;
using Tito.Api.ErrorHandling;
using Tito.Api.Features.Attendance;
using Tito.Core.Common;
using Tito.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ======================= 1. SERVICES (dependency injection) =======================

builder.AddServiceDefaults();                          // OpenTelemetry + /health checks
builder.AddSqlServerDbContext<TitoDbContext>("tito");  // DbContext (scoped) + retries + DB health check

// ---- Options: typed, validated settings. App fails to start if they're invalid. ----
builder.Services.AddOptions<CompanyOptions>()
    .BindConfiguration(CompanyOptions.Section)
    .ValidateDataAnnotations()
    .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _),
        "Company:TimeZone is not a known time zone id (use IANA ids like Europe/Stockholm).")
    .ValidateOnStart();
builder.Services.AddOptions<RateLimitOptions>()
    .BindConfiguration(RateLimitOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---- Time: one clock for the whole app. Tests replace TimeProvider with a fake. ----
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new CompanyClock(
    sp.GetRequiredService<TimeProvider>(),
    TimeZoneInfo.FindSystemTimeZoneById(sp.GetRequiredService<IOptions<CompanyOptions>>().Value.TimeZone)));

// ---- Application services ----
builder.Services.AddScoped<AttendanceService>();       // scoped = one per HTTP request (like DbContext)

// ---- Web API ----
// One JSON setup, applied to BOTH option sets: MVC uses it to (de)serialize controller
// bodies, and the OpenAPI generator uses the "HTTP" set to describe schemas. If they
// differ, the published contract lies (e.g. enums documented as integers).
static void ConfigureJson(JsonSerializerOptions json)
{
    json.Converters.Add(new JsonStringEnumConverter());               // enums as "Working", not 1
    json.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow; // unknown fields -> 400
    json.NumberHandling = JsonNumberHandling.Strict;                   // numbers are numbers, never "42"
}
builder.Services.ConfigureHttpJsonOptions(o => ConfigureJson(o.SerializerOptions));
builder.Services.AddControllers().AddJsonOptions(o => ConfigureJson(o.JsonSerializerOptions));
builder.Services.AddProblemDetails();                  // standard error JSON for all errors
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();                         // builds /openapi/v1.json from controllers + XML comments

// ---- CORS: which browser origins may call us (the Angular app). ----
var corsOrigins = builder.Configuration.GetSection(CorsOptions.Section).Get<CorsOptions>()?.AllowedOrigins ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .WithMethods("GET", "POST")
    .AllowAnyHeader()));

// ---- Rate limiting: N write requests per minute per client IP. ----
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(RateLimitPolicies.Writes, http =>
    {
        var limit = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value.WritesPerMinute;
        var clientIp = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = TimeSpan.FromMinutes(1),
        });
    });
});

var app = builder.Build();

// ======================= 2. MIDDLEWARE PIPELINE (order matters) =======================

app.UseExceptionHandler();                             // outermost: catches errors from everything below

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                  // GET /openapi/v1.json
    app.MapScalarApiReference();                       // GET /scalar  -> interactive API docs

    // Apply pending migrations locally. In Azure the pipeline does this (Phase 7).
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<TitoDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.MapControllers();
app.MapDefaultEndpoints();                             // /health and /alive

app.Run();
