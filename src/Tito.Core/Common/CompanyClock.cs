namespace Tito.Core.Common;

/// <summary>
/// Single source of "now" and "today" for the whole app, in the company's time zone.
/// </summary>
public sealed class CompanyClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
{
    public TimeZoneInfo TimeZone { get; } = timeZone;

    /// <summary>Current instant in UTC (what we store in the database).</summary>
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>The company's calendar date right now.</summary>
    public DateOnly Today => ToCompanyDate(UtcNow);

    /// <summary>Converts a UTC instant to the company's calendar date.</summary>
    public DateOnly ToCompanyDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone));

    /// <summary>Converts a UTC instant to company local wall-clock time.</summary>
    public DateTime ToCompanyTime(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
}
