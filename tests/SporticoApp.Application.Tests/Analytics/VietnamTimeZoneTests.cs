using SporticoApp.Shared.Helpers;
using Xunit;

namespace SporticoApp.Application.Tests.Analytics;

/// <summary>
/// Deterministic boundary tests for VN-business-day calculations. These are the exact same
/// calculations used for RevenueToday/ThisWeek/ThisMonth/ThisYear and for the admin chart bucket
/// boundaries (the SQL side uses the equivalent Postgres idiom, verified separately against a real
/// Postgres instance — see the audit report).
/// </summary>
public class VietnamTimeZoneTests
{
    // 2026-07-21 17:30:00 UTC = 2026-07-22 00:30:00 VN (UTC+7) — already past VN midnight, i.e.
    // still "yesterday" in UTC terms but already "today" (the 22nd) in VN business time.
    private static readonly DateTime UtcJustAfterVnMidnight =
        new(2026, 7, 21, 17, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void StartOfDayUtc_RecordJustAfterVnMidnight_BucketsIntoTheNewVnDay()
    {
        var dayStart = VietnamTimeZone.StartOfDayUtc(UtcJustAfterVnMidnight);

        // VN-local July 22 00:00 == UTC July 21 17:00 — NOT UTC July 21 00:00 (the naive/wrong answer).
        Assert.Equal(new DateTime(2026, 7, 21, 17, 0, 0, DateTimeKind.Utc), dayStart);
        Assert.NotEqual(UtcJustAfterVnMidnight.Date, dayStart);
    }

    [Fact]
    public void StartOfDayUtc_RecordBeforeVnMidnight_StaysInTheSameVnDay()
    {
        // 2026-07-21 16:00 UTC = 2026-07-21 23:00 VN — still July 21 in VN.
        var utc = new DateTime(2026, 7, 21, 16, 0, 0, DateTimeKind.Utc);

        var dayStart = VietnamTimeZone.StartOfDayUtc(utc);

        Assert.Equal(new DateTime(2026, 7, 20, 17, 0, 0, DateTimeKind.Utc), dayStart);
    }

    [Fact]
    public void StartOfWeekUtc_StartsOnMonday()
    {
        // 2026-07-22 is a Wednesday (VN-local, from UtcJustAfterVnMidnight).
        var weekStart = VietnamTimeZone.StartOfWeekUtc(UtcJustAfterVnMidnight);
        var weekStartVn = TimeZoneInfo.ConvertTimeFromUtc(
            weekStart, TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZone.IanaId));

        Assert.Equal(DayOfWeek.Monday, weekStartVn.DayOfWeek);
        Assert.Equal(new DateTime(2026, 7, 20), weekStartVn.Date); // Monday 2026-07-20
    }

    [Fact]
    public void StartOfMonthUtc_UsesVnLocalMonth()
    {
        var monthStart = VietnamTimeZone.StartOfMonthUtc(UtcJustAfterVnMidnight);

        // VN-local July 1 00:00 == UTC June 30 17:00.
        Assert.Equal(new DateTime(2026, 6, 30, 17, 0, 0, DateTimeKind.Utc), monthStart);
    }

    [Fact]
    public void StartOfYearUtc_UsesVnLocalYear()
    {
        var yearStart = VietnamTimeZone.StartOfYearUtc(UtcJustAfterVnMidnight);

        Assert.Equal(new DateTime(2025, 12, 31, 17, 0, 0, DateTimeKind.Utc), yearStart);
    }

    [Fact]
    public void NoDaylightSavingTime_OffsetIsFixedSevenHours()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZone.IanaId);

        Assert.False(zone.SupportsDaylightSavingTime);
        Assert.Equal(TimeSpan.FromHours(7), zone.BaseUtcOffset);
    }
}
