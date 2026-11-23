using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MsSql;

namespace Tito.Api.Tests;

/// <summary>
/// Runs the real API in memory against a throwaway SQL Server container.
/// xUnit creates one per test class (IClassFixture) and disposes it at the end.
/// </summary>
public sealed class TitoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>"Now" for the API: 20 Nov 2026 08:00 UTC = 09:00 in Stockholm.</summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero));

    public static readonly DateOnly Today = new(2026, 11, 20);

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    // Called by xUnit before any test: start SQL Server in Docker.
    public Task InitializeAsync() => _sql.StartAsync();

    // Called by xUnit after all tests: remove the container.
    Task IAsyncLifetime.DisposeAsync() => _sql.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");   // so Program.cs applies migrations
        builder.UseSetting("ConnectionStrings:tito", _sql.GetConnectionString());
        builder.UseSetting("Company:TimeZone", "Europe/Stockholm");
        builder.UseSetting("RateLimit:WritesPerMinute", "1000");

        // Replace the real clock with our fake one.
        builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock)));
    }
}
