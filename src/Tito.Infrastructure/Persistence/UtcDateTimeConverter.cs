using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tito.Infrastructure.Persistence;

/// <summary>Writes DateTime unchanged; on read, stamps it as UTC (DateTimeKind.Utc).</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDb => toDb,
    fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));
