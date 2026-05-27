using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPlans;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
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
        private readonly IValidator<CreateTrainingPlanRequest> _createValidator;
        private readonly IValidator<UpdateTrainingPlanRequest> _updateValidator;
        private readonly IValidator<CreateTrainingPlanWeekRequest> _weekValidator;
        private readonly IValidator<CreateTrainingPlanDayRequest> _dayValidator;
        private readonly IValidator<CreateTrainingPlanExerciseRequest> _exerciseValidator;
        private readonly IValidator<UpdateTrainingPlanExerciseRequest> _updateExerciseValidator;

        public TrainingPlanService(
            IBookingRepository bookingRepository,
            ITrainingPlanRepository trainingPlanRepository,
            IValidator<CreateTrainingPlanRequest> createValidator,
            IValidator<UpdateTrainingPlanRequest> updateValidator,
            IValidator<CreateTrainingPlanWeekRequest> weekValidator,
            IValidator<CreateTrainingPlanDayRequest> dayValidator,
            IValidator<CreateTrainingPlanExerciseRequest> exerciseValidator,
            IValidator<UpdateTrainingPlanExerciseRequest> updateExerciseValidator)
        {
            _bookingRepository = bookingRepository;
            _trainingPlanRepository = trainingPlanRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _weekValidator = weekValidator;
            _dayValidator = dayValidator;
            _exerciseValidator = exerciseValidator;
            _updateExerciseValidator = updateExerciseValidator;
        }

        public async Task<Result<TrainingPlanResponse>> CreateAsync(
            Guid coachId,
            Guid bookingId,
            CreateTrainingPlanRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
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

            var existingPlan = await _trainingPlanRepository.GetByBookingIdAsync(bookingId);
            if (existingPlan != null)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Training plan already exists");
            }

            var plan = request.ToEntity(bookingId, booking.LearnerId, booking.CoachId);

            await _trainingPlanRepository.AddAsync(plan);

            return Result<TrainingPlanResponse>.Success(plan.ToResponse());
        }

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

            return Result<TrainingPlanResponse>.Success(plan.ToResponse());
        }

        public async Task<Result<TrainingPlanResponse>> UpdateAsync(
            Guid coachId,
            Guid planId,
            UpdateTrainingPlanRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
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

            plan.ApplyUpdate(request);

            await _trainingPlanRepository.SaveChangesAsync();

            return Result<TrainingPlanResponse>.Success(plan.ToResponse());
        }

        public async Task<Result<TrainingPlanWeekResponse>> AddWeekAsync(
            Guid coachId,
            Guid planId,
            CreateTrainingPlanWeekRequest request)
        {
            var validationResult = await _weekValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
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

            var week = request.ToEntity(plan.Id);

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
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
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

            var day = request.ToEntity(week.Id);

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
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var day = await _trainingPlanRepository.GetDayByIdForUpdateAsync(dayId);
            if (day == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPlanNotFound,
                    "Training plan day not found");
            }

            if (day.TrainingPlanWeek.TrainingPlan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            var exercise = request.ToEntity(day.Id);

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
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
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

            if (day.TrainingPlanWeek.TrainingPlan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            exercise.ApplyUpdate(request);

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

            if (day.TrainingPlanWeek.TrainingPlan.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingPlanNotOwned,
                    "Training plan is not owned by the current coach");
            }

            await _trainingPlanRepository.RemoveExercise(exercise);
            await _trainingPlanRepository.SaveChangesAsync();

            return Result<object>.Success(new { status = "ok" });
        }
    }
}
