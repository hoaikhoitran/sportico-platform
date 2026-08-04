using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CommunityCommentRepository : ICommunityCommentRepository
    {
        private readonly AppDbContext _context;

        public CommunityCommentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommunityComment?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.CommunityComments
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<CommunityComment> Items, int TotalCount)> GetRootCommentsPagedAsync(
            Guid postId, CommunityCommentFilterRequest filter)
        {
            IQueryable<CommunityComment> query = _context.CommunityComments
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Replies.OrderBy(r => r.CreatedAt))
                    .ThenInclude(r => r.Author)
                .Where(x =>
                    x.PostId == postId &&
                    x.ParentCommentId == null &&
                    x.Status != CommunityCommentStatuses.Deleted);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<CommunityComment> Items, int TotalCount)> GetForAdminPagedAsync(
            Guid postId, CommunityCommentFilterRequest filter)
        {
            IQueryable<CommunityComment> query = _context.CommunityComments
                .AsNoTracking()
                .Include(x => x.Author)
                .Where(x => x.PostId == postId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task AddWithoutSaveAsync(CommunityComment comment)
        {
            _context.CommunityComments.Add(comment);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
