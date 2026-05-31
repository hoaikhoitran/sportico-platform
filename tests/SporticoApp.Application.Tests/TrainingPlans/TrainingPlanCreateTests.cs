using SporticoApp.Application.DTOs.TrainingPlans;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments;
using SporticoApp.Application.Validators.TrainingPlans;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.TrainingPlans;

/// <summary>
/// Covers the coach-created training-plan gate: a plan may only be created for an
/// active, coach-owned booking, exactly once. The 409 on a pending_payment booking is
/// the *correct* behaviour — the real bug was elsewhere (payment activation).
/// </summary>
public class TrainingPlanCreateTests
{
    private static readonly Guid CoachId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid LearnerId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static CreateTrainingPlanRequest ValidRequest() => new()
    {
        Title = "12-week plan",
        GoalType = "strength",
        Overview = "Build base strength",
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(84),
        TotalWeeks = 12
    };

    private static Booking BookingWith(string status) => new()
    {
        Id = Guid.NewGuid(),
        LearnerId = LearnerId,
        CoachId = CoachId,
        TrainingPackageId = Guid.NewGuid(),
        Status = status,
        ExpiresAt = DateTime.UtcNow.AddDays(30),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static TrainingPlanService BuildService(
        FakePlanBookingRepository bookings,
        FakeTrainingPlanRepository plans)
        => new(
            bookings,
            plans,
            new FakeNotificationRepository(),
            new CreateTrainingPlanRequestValidator(),
            new PassValidator<UpdateTrainingPlanRequest>(),
            new PassValidator<CreateTrainingPlanWeekRequest>(),
            new PassValidator<CreateTrainingPlanDayRequest>(),
            new PassValidator<CreateTrainingPlanExerciseRequest>(),
            new PassValidator<UpdateTrainingPlanExerciseRequest>());

    // 5. Coach can create a training plan when the booking is active.
    [Fact]
    public async Task Create_ActiveBooking_Succeeds()
    {
        var booking = BookingWith(BookingStatuses.Active);
        var plans = new FakeTrainingPlanRepository();
        var service = BuildService(new FakePlanBookingRepository(booking, booking), plans);

        var result = await service.CreateAsync(CoachId, booking.Id, ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Single(plans.Added);
    }

    // 6. Coach cannot create a plan when the booking is pending_payment → 409 (ConflictException).
    [Fact]
    public async Task Create_PendingPaymentBooking_ThrowsConflict()
    {
        var booking = BookingWith(BookingStatuses.PendingPayment);
        var plans = new FakeTrainingPlanRepository();
        var service = BuildService(new FakePlanBookingRepository(booking, booking), plans);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CoachId, booking.Id, ValidRequest()));

        Assert.Equal(ErrorCodes.BookingNotActive, ex.Code);
        Assert.Empty(plans.Added);
    }

    // 7. A coach who does not own the booking cannot create a plan → Forbidden.
    [Fact]
    public async Task Create_NotOwnedBooking_ThrowsForbidden()
    {
        var booking = BookingWith(BookingStatuses.Active);
        var plans = new FakeTrainingPlanRepository();
        // owned lookup returns null (different coach); existence lookup finds it.
        var service = BuildService(new FakePlanBookingRepository(owned: null, any: booking), plans);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(Guid.NewGuid(), booking.Id, ValidRequest()));

        Assert.Empty(plans.Added);
    }

    // 8. A second plan cannot be created for the same booking → 409.
    [Fact]
    public async Task Create_SecondPlanForSameBooking_ThrowsConflict()
    {
        var booking = BookingWith(BookingStatuses.Active);
        var plans = new FakeTrainingPlanRepository
        {
            Existing = new TrainingPlan { Id = Guid.NewGuid(), BookingId = booking.Id }
        };
        var service = BuildService(new FakePlanBookingRepository(booking, booking), plans);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(CoachId, booking.Id, ValidRequest()));

        Assert.Empty(plans.Added);
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakePlanBookingRepository : IBookingRepository
    {
        private readonly Booking? _owned;
        private readonly Booking? _any;

        public FakePlanBookingRepository(Booking? owned, Booking? any)
        {
            _owned = owned;
            _any = any;
        }

        public Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id)
            => Task.FromResult(_owned != null && _owned.Id == id && _owned.CoachId == coachId ? _owned : null);

        public Task<Booking?> GetByIdAsync(Guid id)
            => Task.FromResult(_any != null && _any.Id == id ? _any : null);

        public Task<Booking?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, SporticoApp.Application.DTOs.Bookings.BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, SporticoApp.Application.DTOs.Bookings.BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(SporticoApp.Application.DTOs.Bookings.BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(Guid learnerId, Guid coachId) => throw new NotImplementedException();
        public Task AddAsync(Booking booking) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Booking booking) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeTrainingPlanRepository : ITrainingPlanRepository
    {
        public TrainingPlan? Existing;
        public readonly List<TrainingPlan> Added = new();

        public Task<TrainingPlan?> GetByBookingIdAsync(Guid bookingId) => Task.FromResult(Existing);
        public Task AddAsync(TrainingPlan plan)
        {
            Added.Add(plan);
            return Task.CompletedTask;
        }

        public Task<TrainingPlan?> GetByBookingIdForUpdateAsync(Guid bookingId) => throw new NotImplementedException();
        public Task<TrainingPlan?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingPlan?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingPlanWeek?> GetWeekByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingPlanDay?> GetDayByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<TrainingPlanExercise?> GetExerciseByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task AddWeekAsync(TrainingPlanWeek week) => throw new NotImplementedException();
        public Task AddDayAsync(TrainingPlanDay day) => throw new NotImplementedException();
        public Task AddExerciseAsync(TrainingPlanExercise exercise) => throw new NotImplementedException();
        public Task RemoveExercise(TrainingPlanExercise exercise) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
