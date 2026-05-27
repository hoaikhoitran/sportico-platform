using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class LearnerAssessmentRepository : ILearnerAssessmentRepository
    {
        private readonly AppDbContext _context;

        public LearnerAssessmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LearnerAssessment?> GetByBookingIdAsync(Guid bookingId)
        {
            return await _context.LearnerAssessments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);
        }

        public async Task<LearnerAssessment?> GetByBookingIdForUpdateAsync(Guid bookingId)
        {
            return await _context.LearnerAssessments
                .FirstOrDefaultAsync(x => x.BookingId == bookingId);
        }

        public async Task AddAsync(LearnerAssessment assessment)
        {
            _context.LearnerAssessments.Add(assessment);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
