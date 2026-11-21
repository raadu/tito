using Microsoft.EntityFrameworkCore;
using Tito.Core.Attendance;
using Tito.Core.Employees;

namespace Tito.Infrastructure.Persistence;

/// <summary>EF Core's unit of work: tracks changes to entities and saves them as SQL.</summary>
public sealed class TitoDbContext(DbContextOptions<TitoDbContext> options) : DbContext(options)
{
    // Each DbSet = one table. `=> Set<T>()` avoids needing a setter.
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Finds every IEntityTypeConfiguration<T> class in this assembly (see Configurations/).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TitoDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQL `datetime2` doesn't store "this is UTC". When EF reads it back, mark it as UTC
        // so JSON output ends with "Z" and browsers convert to local time correctly.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
