using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface ILearnerAssessmentRepository
    {
        Task<LearnerAssessment?> GetByBookingIdAsync(Guid bookingId);

        Task<LearnerAssessment?> GetByBookingIdForUpdateAsync(Guid bookingId);

        Task AddAsync(LearnerAssessment assessment);

        Task SaveChangesAsync();
    }
}
