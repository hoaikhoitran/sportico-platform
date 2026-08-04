using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CommunityPostApplicationRepository : ICommunityPostApplicationRepository
    {
        private readonly AppDbContext _context;

        public CommunityPostApplicationRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<CommunityPostApplication?> GetByIdForUpdateAsync(Guid id)
        {
            return _context.CommunityPostApplications.FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<CommunityPostApplication?> GetByPostAndApplicantAsync(Guid postId, Guid applicantId)
        {
            return _context.CommunityPostApplications
                .FirstOrDefaultAsync(x => x.PostId == postId && x.ApplicantId == applicantId);
        }

        public async Task<(List<CommunityPostApplication> Items, int TotalCount)> GetPagedByPostAsync(
            Guid postId, CommunityApplicationFilterRequest filter)
        {
            IQueryable<CommunityPostApplication> query = _context.CommunityPostApplications
                .AsNoTracking()
                .Include(x => x.Applicant)
                .Where(x => x.PostId == postId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task AddWithoutSaveAsync(CommunityPostApplication application)
        {
            _context.CommunityPostApplications.Add(application);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
