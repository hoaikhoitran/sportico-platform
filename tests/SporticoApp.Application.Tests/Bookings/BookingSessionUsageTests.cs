using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Bookings;

/// <summary>
/// The single source of truth for booking session usage. Rule (BookingQuotaConsuming):
/// requested + scheduled + completed + missed consume a slot; cancelled does not.
/// </summary>
public class BookingSessionUsageTests
{
    private static Dictionary<string, int> Counts(
        int requested = 0, int scheduled = 0, int completed = 0, int cancelled = 0, int missed = 0)
    {
        var d = new Dictionary<string, int>();
        if (requested > 0) d[TrainingSessionStatuses.Requested] = requested;
        if (scheduled > 0) d[TrainingSessionStatuses.Scheduled] = scheduled;
        if (completed > 0) d[TrainingSessionStatuses.Completed] = completed;
        if (cancelled > 0) d[TrainingSessionStatuses.Cancelled] = cancelled;
        if (missed > 0) d[TrainingSessionStatuses.Missed] = missed;
        return d;
    }

    [Fact]
    public void NoSessions_AllRemaining_CanBook()
    {
        var u = BookingSessionUsage.From(3, Counts());
        Assert.Equal(3, u.TotalSessions);
        Assert.Equal(0, u.UsedSessions);
        Assert.Equal(0, u.CompletedSessions);
        Assert.Equal(3, u.RemainingSessions);
        Assert.True(u.CanBookSession);
    }

    [Fact]
    public void TwoCompleted_OfThree_OneRemaining_CanBook()
    {
        var u = BookingSessionUsage.From(3, Counts(completed: 2));
        Assert.Equal(2, u.UsedSessions);
        Assert.Equal(2, u.CompletedSessions);
        Assert.Equal(1, u.RemainingSessions);
        Assert.True(u.CanBookSession);
    }

    [Fact]
    public void ThreeCompleted_OfThree_NoneRemaining_CannotBook()
    {
        var u = BookingSessionUsage.From(3, Counts(completed: 3));
        Assert.Equal(3, u.UsedSessions);
        Assert.Equal(3, u.CompletedSessions);
        Assert.Equal(0, u.RemainingSessions);
        Assert.False(u.CanBookSession);
    }

    [Fact]
    public void RequestedAndScheduled_ConsumeSlots_EvenBeforeCompletion()
    {
        // 1 requested + 2 scheduled = 3 used → full, although 0 completed.
        var u = BookingSessionUsage.From(3, Counts(requested: 1, scheduled: 2));
        Assert.Equal(3, u.UsedSessions);
        Assert.Equal(0, u.CompletedSessions);
        Assert.Equal(0, u.RemainingSessions);
        Assert.False(u.CanBookSession);
    }

    [Fact]
    public void Cancelled_DoesNotConsumeSlot()
    {
        var u = BookingSessionUsage.From(3, Counts(cancelled: 5));
        Assert.Equal(0, u.UsedSessions);
        Assert.Equal(3, u.RemainingSessions);
        Assert.True(u.CanBookSession);
    }

    [Fact]
    public void Missed_ConsumesSlot()
    {
        var u = BookingSessionUsage.From(3, Counts(completed: 1, missed: 1));
        Assert.Equal(2, u.UsedSessions);     // completed + missed
        Assert.Equal(1, u.CompletedSessions); // only completed
        Assert.Equal(1, u.RemainingSessions);
        Assert.True(u.CanBookSession);
    }

    [Fact]
    public void Overconsumption_ClampsRemainingToZero()
    {
        var u = BookingSessionUsage.From(3, Counts(requested: 5));
        Assert.Equal(5, u.UsedSessions);
        Assert.Equal(0, u.RemainingSessions); // never negative
        Assert.False(u.CanBookSession);
    }

    [Fact]
    public void MixedStatuses_CombineCorrectly()
    {
        // total 5: 1 completed + 1 scheduled + 1 requested + 1 missed = 4 used; 1 cancelled ignored.
        var u = BookingSessionUsage.From(5, Counts(requested: 1, scheduled: 1, completed: 1, cancelled: 1, missed: 1));
        Assert.Equal(4, u.UsedSessions);
        Assert.Equal(1, u.CompletedSessions);
        Assert.Equal(1, u.RemainingSessions);
        Assert.True(u.CanBookSession);
    }
}
