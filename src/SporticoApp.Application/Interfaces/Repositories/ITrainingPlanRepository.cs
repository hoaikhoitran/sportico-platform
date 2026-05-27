using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ITrainingPlanRepository
    {
        Task<TrainingPlan?> GetByBookingIdAsync(Guid bookingId);

        Task<TrainingPlan?> GetByBookingIdForUpdateAsync(Guid bookingId);

        Task<TrainingPlan?> GetByIdAsync(Guid id);

        Task<TrainingPlan?> GetByIdForUpdateAsync(Guid id);

        Task<TrainingPlanWeek?> GetWeekByIdForUpdateAsync(Guid id);

        Task<TrainingPlanDay?> GetDayByIdForUpdateAsync(Guid id);

        Task<TrainingPlanExercise?> GetExerciseByIdForUpdateAsync(Guid id);

        Task AddAsync(TrainingPlan plan);

        Task AddWeekAsync(TrainingPlanWeek week);

        Task AddDayAsync(TrainingPlanDay day);

        Task AddExerciseAsync(TrainingPlanExercise exercise);

        Task RemoveExercise(TrainingPlanExercise exercise);

        Task SaveChangesAsync();
    }
}
