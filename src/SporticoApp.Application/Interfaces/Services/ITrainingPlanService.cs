using SporticoApp.Application.DTOs.TrainingPlans;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ITrainingPlanService
    {
        Task<Result<TrainingPlanResponse>> CreateAsync(
            Guid coachId,
            Guid bookingId,
            CreateTrainingPlanRequest request);

        Task<Result<TrainingPlanResponse>> GetByBookingAsync(
            Guid userId,
            Guid bookingId);

        Task<Result<TrainingPlanResponse>> UpdateAsync(
            Guid coachId,
            Guid planId,
            UpdateTrainingPlanRequest request);

        Task<Result<TrainingPlanWeekResponse>> AddWeekAsync(
            Guid coachId,
            Guid planId,
            CreateTrainingPlanWeekRequest request);

        Task<Result<TrainingPlanDayResponse>> AddDayAsync(
            Guid coachId,
            Guid weekId,
            CreateTrainingPlanDayRequest request);

        Task<Result<TrainingPlanExerciseResponse>> AddExerciseAsync(
            Guid coachId,
            Guid dayId,
            CreateTrainingPlanExerciseRequest request);

        Task<Result<TrainingPlanExerciseResponse>> UpdateExerciseAsync(
            Guid coachId,
            Guid exerciseId,
            UpdateTrainingPlanExerciseRequest request);

        Task<Result<object>> DeleteExerciseAsync(
            Guid coachId,
            Guid exerciseId);
    }
}
