using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class TrainingPlanRepository : ITrainingPlanRepository
    {
        private readonly AppDbContext _context;

        public TrainingPlanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TrainingPlan?> GetByBookingIdAsync(Guid bookingId)
        {
            return await _context.TrainingPlans
                .AsNoTracking()
                .Include(x => x.Weeks)
                .ThenInclude(x => x.Days)
                .ThenInclude(x => x.Exercises)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);
        }

        public async Task<TrainingPlan?> GetByBookingIdForUpdateAsync(Guid bookingId)
        {
            return await _context.TrainingPlans
                .Include(x => x.Weeks)
                .ThenInclude(x => x.Days)
                .ThenInclude(x => x.Exercises)
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);
        }

        public async Task<TrainingPlan?> GetByIdAsync(Guid id)
        {
            return await _context.TrainingPlans
                .AsNoTracking()
                .Include(x => x.Weeks)
                .ThenInclude(x => x.Days)
                .ThenInclude(x => x.Exercises)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingPlan?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.TrainingPlans
                .Include(x => x.Weeks)
                .ThenInclude(x => x.Days)
                .ThenInclude(x => x.Exercises)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingPlanWeek?> GetWeekByIdForUpdateAsync(Guid id)
        {
            return await _context.TrainingPlanWeeks
                .Include(x => x.TrainingPlan)
                .Include(x => x.Days)
                .ThenInclude(x => x.Exercises)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingPlanDay?> GetDayByIdForUpdateAsync(Guid id)
        {
            return await _context.TrainingPlanDays
                .Include(x => x.TrainingPlanWeek)
                .ThenInclude(x => x.TrainingPlan)
                .Include(x => x.Exercises)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<TrainingPlanExercise?> GetExerciseByIdForUpdateAsync(Guid id)
        {
            return await _context.TrainingPlanExercises
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(TrainingPlan plan)
        {
            _context.TrainingPlans.Add(plan);
            await _context.SaveChangesAsync();
        }

        public async Task AddWeekAsync(TrainingPlanWeek week)
        {
            _context.TrainingPlanWeeks.Add(week);
            await _context.SaveChangesAsync();
        }

        public async Task AddDayAsync(TrainingPlanDay day)
        {
            _context.TrainingPlanDays.Add(day);
            await _context.SaveChangesAsync();
        }

        public async Task AddExerciseAsync(TrainingPlanExercise exercise)
        {
            _context.TrainingPlanExercises.Add(exercise);
            await _context.SaveChangesAsync();
        }

        public Task RemoveExercise(TrainingPlanExercise exercise)
        {
            _context.TrainingPlanExercises.Remove(exercise);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
