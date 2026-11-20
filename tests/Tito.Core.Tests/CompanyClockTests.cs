using Microsoft.Extensions.Time.Testing;
using Tito.Core.Common;

namespace Tito.Core.Tests;

public class CompanyClockTests
{
    [Fact]
    public void Today_uses_company_time_zone_not_utc()
    {
        // 20 Nov 20:00 UTC == 21 Nov 02:00 in Dhaka (UTC+6)
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 20, 0, 0, TimeSpan.Zero));
        var clock = new CompanyClock(fakeTime, TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka"));

        Assert.Equal(new DateOnly(2026, 11, 21), clock.Today);
    }

    [Fact]
    public void UtcNow_follows_the_time_provider()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));
        var clock = new CompanyClock(fakeTime, TimeZoneInfo.Utc);

        fakeTime.Advance(TimeSpan.FromMinutes(90)); // move the fake clock forward

        Assert.Equal(new DateTime(2026, 11, 20, 10, 30, 0, DateTimeKind.Utc), clock.UtcNow);
    }
}
