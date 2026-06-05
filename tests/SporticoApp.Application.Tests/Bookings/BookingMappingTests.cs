using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Bookings;

/// <summary>Booking → response mapping must prefer real usage over the denormalized counter.</summary>
public class BookingMappingTests
{
    [Fact]
    public void ToResponse_WithUsage_OverridesStaleCompletedSessions()
    {
        // Denormalized counter is stale (says 2) ...
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TotalSessions = 3,
            CompletedSessions = 2,
            Status = BookingStatuses.Active
        };

        // ... but real usage says all 3 are completed.
        var usage = BookingSessionUsage.From(3, new Dictionary<string, int>
        {
            [TrainingSessionStatuses.Completed] = 3
        });

        var response = booking.ToResponse(usage);

        Assert.Equal(3, response.TotalSessions);
        Assert.Equal(3, response.CompletedSessions);   // real, not the stale 2
        Assert.Equal(3, response.UsedSessions);
        Assert.Equal(0, response.RemainingSessions);
        Assert.False(response.CanBookSession);
        Assert.NotNull(response.SessionCountsByStatus);
    }

    [Fact]
    public void ToResponse_BaseOverload_FallsBackToCounterButFillsUsageFields()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TotalSessions = 5,
            CompletedSessions = 1,
            Status = BookingStatuses.Active
        };

        var response = booking.ToResponse();

        // Base mapping still yields sensible (counter-based) usage fields so the contract is populated.
        Assert.Equal(5, response.TotalSessions);
        Assert.Equal(1, response.CompletedSessions);
        Assert.Equal(1, response.UsedSessions);
        Assert.Equal(4, response.RemainingSessions);
        Assert.True(response.CanBookSession);
    }
}
