using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPlans;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class TrainingPlanService : ITrainingPlanService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITrainingPlanRepository _trainingPlanRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CreateTrainingPlanRequest> _createValidator;
        private readonly IValidator<UpdateTrainingPlanRequest> _updateValidator;
        private readonly IValidator<CreateTrainingPlanWeekRequest> _weekValidator;
        private readonly IValidator<CreateTrainingPlanDayRequest> _dayValidator;
        private readonly IValidator<CreateTrainingPlanExerciseRequest> _exerciseValidator;
        private readonly IValidator<UpdateTrainingPlanExerciseRequest> _updateExerciseValidator;

        public TrainingPlanService(
            IBookingRepository bookingRepository,
            ITrainingPlanRepository trainingPlanRepository,
            INotificationRepository notificationRepository,
            IValidator<CreateTrainingPlanRequest> createValidator,
            IValidator<UpdateTrainingPlanRequest> updateValidator,
            IValidator<CreateTrainingPlanWeekRequest> weekValidator,
            IValidator<CreateTrainingPlanDayRequest> dayValidator,
            IValidator<CreateTrainingPlanExerciseRequest> exerciseValidator,
            IValidator<UpdateTrainingPlanExerciseRequest> updateExerciseValidator)
        {
            _bookingRepository = bookingRepository;
            _trainingPlanRepository = trainingPlanRepository;
            _notificationRepository = notificationRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _weekValidator = weekValidator;
            _dayValidator = dayValidator;
            _exerciseValidator = exerciseValidator;
            _updateExerciseValidator = updateExerciseValidator;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Create — coach only, active & non-expired booking, one plan per booking
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<TrainingPlanResponse>> CreateAsync(
            Guid coachId,
            Guid bookingId,
            CreateTrainingPlanRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var booking = await _bookingRepository.GetByIdForCoachAsync(coachId, bookingId);
            if (booking == null)
            {
                var existing = await _bookingRepository.GetByIdAsync(bookingId);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.BookingNotOwned,
                        "Booking is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            if (booking.Status != BookingStatuses.Active)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Booking is not active");
            }

            // Block creation once the purchased package has expired.
            if (booking.ExpiresAt is { } expiresAt && DateTime.UtcNow > expiresAt)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Training package has expired. Cannot create a training plan.");
            }

            var existingPlan = await _trainingPlanRepository.GetByBookingIdAsync(bookingId);
            if (existingPlan != null)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Training plan already exists");
            }

            var plan = request.ToEntity(bookingId, booking.LearnerId, booking.CoachId);

            await _trainingPlanRepository.AddAsync(plan);

            await NotifyLearnerAsync(
                booking.LearnerId,
                "Training plan created",
                $"Your coach has created a training plan: {plan.Title}");

            return Result<TrainingPlanResponse>.Success(plan.ToResponse(booking.ExpiresAt));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Read — learner or coach of the booking. Always allowed (even if expired).
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<TrainingPlanResponse>> GetByBookingAsync(
            Guid userId,
            Guid bookingId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            if (booking.LearnerId != userId && booking.CoachId != userId)
            {
                throw new ForbiddenException(
                    ErrorCodes.BookingNotOwned,
                    "Booking is not accessible by the current user");
            }

            var plan = await _trainingPlanRepository.GetByBookingIdAsync(bookingId);
            if (plan == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan not found");
            }

            return Result<TrainingPlanResponse>.Success(plan.ToResponse(booking.ExpiresAt));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Update plan metadata + status — coach owner only, editable plans only
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<TrainingPlanResponse>> UpdateAsync(
            Guid coachId,
            Guid planId,
            UpdateTrainingPlanRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var plan = await _trainingPlanRepository.GetByIdForUpdateAsync(planId);
            if (plan == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan not found");
            }

            if (plan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var booking = await _bookingRepository.GetByIdAsync(plan.BookingId);
            EnsureEditable(plan, booking);

            // Validate & apply status transition before metadata update.
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var newStatus = request.Status.Trim().ToLowerInvariant();
                if (!TrainingPlanStatuses.All.Contains(newStatus))
                {
                    throw new ValidationException(
                        ErrorCodes.InvalidTrainingPlanStatus,
                        "Invalid training plan status");
                }

                if (!IsValidTransition(plan.Status, newStatus))
                {
                    throw new ConflictException(
                        ErrorCodes.InvalidTrainingPlanStatus,
                        "Training plan status transition is not allowed");
                }

                plan.Status = newStatus;
            }

            plan.ApplyUpdate(request);

            await _trainingPlanRepository.SaveChangesAsync();

            await NotifyLearnerAsync(
                plan.LearnerId,
                "Training plan updated",
                "Your training plan was updated");

            return Result<TrainingPlanResponse>.Success(plan.ToResponse(booking?.ExpiresAt));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Nested mutations — coach owner only, editable plans only
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<TrainingPlanWeekResponse>> AddWeekAsync(
            Guid coachId,
            Guid planId,
            CreateTrainingPlanWeekRequest request)
        {
            var validationResult = await _weekValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var plan = await _trainingPlanRepository.GetByIdForUpdateAsync(planId);
            if (plan == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan not found");
            }

            if (plan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var booking = await _bookingRepository.GetByIdAsync(plan.BookingId);
            EnsureEditable(plan, booking);

            var week = request.ToEntity(plan.Id);

            plan.UpdatedAt = DateTime.UtcNow;
            await _trainingPlanRepository.AddWeekAsync(week);

            return Result<TrainingPlanWeekResponse>.Success(week.ToResponse());
        }

        public async Task<Result<TrainingPlanDayResponse>> AddDayAsync(
            Guid coachId,
            Guid weekId,
            CreateTrainingPlanDayRequest request)
        {
            var validationResult = await _dayValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var week = await _trainingPlanRepository.GetWeekByIdForUpdateAsync(weekId);
            if (week == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan week not found");
            }

            if (week.TrainingPlan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var booking = await _bookingRepository.GetByIdAsync(week.TrainingPlan.BookingId);
            EnsureEditable(week.TrainingPlan, booking);

            var day = request.ToEntity(week.Id);

            week.TrainingPlan.UpdatedAt = DateTime.UtcNow;
            await _trainingPlanRepository.AddDayAsync(day);

            return Result<TrainingPlanDayResponse>.Success(day.ToResponse());
        }

        public async Task<Result<TrainingPlanExerciseResponse>> AddExerciseAsync(
            Guid coachId,
            Guid dayId,
            CreateTrainingPlanExerciseRequest request)
        {
            var validationResult = await _exerciseValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var day = await _trainingPlanRepository.GetDayByIdForUpdateAsync(dayId);
            if (day == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan day not found");
            }

            var plan = day.TrainingPlanWeek.TrainingPlan;
            if (plan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var booking = await _bookingRepository.GetByIdAsync(plan.BookingId);
            EnsureEditable(plan, booking);

            var exercise = request.ToEntity(day.Id);

            plan.UpdatedAt = DateTime.UtcNow;
            await _trainingPlanRepository.AddExerciseAsync(exercise);

            return Result<TrainingPlanExerciseResponse>.Success(exercise.ToResponse());
        }

        public async Task<Result<TrainingPlanExerciseResponse>> UpdateExerciseAsync(
            Guid coachId,
            Guid exerciseId,
            UpdateTrainingPlanExerciseRequest request)
        {
            var validationResult = await _updateExerciseValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var exercise = await _trainingPlanRepository.GetExerciseByIdForUpdateAsync(exerciseId);
            if (exercise == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan exercise not found");
            }

            var day = await _trainingPlanRepository.GetDayByIdForUpdateAsync(exercise.TrainingPlanDayId);
            if (day == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan day not found");
            }

            var plan = day.TrainingPlanWeek.TrainingPlan;
            if (plan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var booking = await _bookingRepository.GetByIdAsync(plan.BookingId);
            EnsureEditable(plan, booking);

            exercise.ApplyUpdate(request);

            plan.UpdatedAt = DateTime.UtcNow;
            await _trainingPlanRepository.SaveChangesAsync();

            return Result<TrainingPlanExerciseResponse>.Success(exercise.ToResponse());
        }

        public async Task<Result<object>> DeleteExerciseAsync(
            Guid coachId,
            Guid exerciseId)
        {
            var exercise = await _trainingPlanRepository.GetExerciseByIdForUpdateAsync(exerciseId);
            if (exercise == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan exercise not found");
            }

            var day = await _trainingPlanRepository.GetDayByIdForUpdateAsync(exercise.TrainingPlanDayId);
            if (day == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan day not found");
            }

            var plan = day.TrainingPlanWeek.TrainingPlan;
            if (plan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var booking = await _bookingRepository.GetByIdAsync(plan.BookingId);
            EnsureEditable(plan, booking);

            plan.UpdatedAt = DateTime.UtcNow;
            await _trainingPlanRepository.RemoveExercise(exercise);
            await _trainingPlanRepository.SaveChangesAsync();

            return Result<object>.Success(new { status = "ok" });
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Private helpers
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Blocks any mutation when the plan is in a terminal status or the underlying
        /// purchased package has expired. Reads are never gated by this method.
        /// </summary>
        private static void EnsureEditable(TrainingPlan plan, Booking? booking)
        {
            if (TrainingPlanStatuses.Terminal.Contains(plan.Status))
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingPlanStatus,
                    $"Training plan is {plan.Status} and can no longer be modified");
            }

            if (booking?.ExpiresAt is { } expiresAt && DateTime.UtcNow > expiresAt)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Training package has expired. Training plan is now read-only.");
            }
        }

        /// <summary>
        /// Allowed transitions:
        ///   draft → active, draft → cancelled,
        ///   active → completed, active → cancelled.
        /// Same-status is a no-op. completed/cancelled are terminal.
        /// </summary>
        private static bool IsValidTransition(string from, string to)
        {
            if (from == to)
            {
                return true;
            }

            return (from, to) switch
            {
                (TrainingPlanStatuses.Draft, TrainingPlanStatuses.Active) => true,
                (TrainingPlanStatuses.Draft, TrainingPlanStatuses.Cancelled) => true,
                (TrainingPlanStatuses.Active, TrainingPlanStatuses.Completed) => true,
                (TrainingPlanStatuses.Active, TrainingPlanStatuses.Cancelled) => true,
                _ => false
            };
        }

        private async Task NotifyLearnerAsync(Guid learnerId, string title, string content)
        {
            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = learnerId,
                Title = title,
                Content = content,
                Type = NotificationTypeConstants.TrainingPlan,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();
        }
    }
}
