using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tito.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` (migrations add / bundle). Never runs inside the app.
/// The connection string is a placeholder: generating migrations doesn't touch a database.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TitoDbContext>
{
    public TitoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TitoDbContext>()
            .UseSqlServer("Server=design-time-only;Database=tito")
            .Options;
        return new TitoDbContext(options);
    }
}
