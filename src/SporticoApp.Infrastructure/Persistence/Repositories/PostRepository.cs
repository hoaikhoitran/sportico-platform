using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Posts;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly AppDbContext _context;

        public PostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(Guid id)
        {
            return await _context.Posts
                .AsNoTracking()
                .Include(x => x.Sport)
                .Include(x => x.PostImages)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Post?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.Posts
                .Include(x => x.PostImages)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Post?> GetOwnedByIdAsync(Guid coachId, Guid postId)
        {
            return await _context.Posts
                .AsNoTracking()
                .Include(x => x.Sport)
                .Include(x => x.PostImages)
                .FirstOrDefaultAsync(x => x.Id == postId && x.CoachId == coachId);
        }

        public async Task<Post?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid postId)
        {
            return await _context.Posts
                .Include(x => x.PostImages)
                .FirstOrDefaultAsync(x => x.Id == postId && x.CoachId == coachId);
        }

        public async Task<(List<Post> Items, int TotalCount)> GetMyPagedAsync(
            Guid coachId,
            PostFilterRequest filter)
        {
            IQueryable<Post> query = _context.Posts
                .AsNoTracking()
                .Include(x => x.Sport)
                .Include(x => x.PostImages)
                .Where(x => x.CoachId == coachId);

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<Post> Items, int TotalCount)> GetAdminPendingPagedAsync(
            PostFilterRequest filter)
        {
            IQueryable<Post> query = _context.Posts
                .AsNoTracking()
                .Include(x => x.Sport)
                .Include(x => x.PostImages);

            if (string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x =>
                    x.Status == PostStatusConstants.Pending ||
                    x.Status == PostStatusConstants.Draft);
            }

            query = ApplyFilter(query, filter);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static IQueryable<Post> ApplyFilter(
            IQueryable<Post> query,
            PostFilterRequest filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var normalized = filter.Keyword.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(normalized) ||
                    (x.Description != null && x.Description.ToLower().Contains(normalized)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var status = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status.ToLower() == status);
            }

            if (filter.SportId.HasValue)
            {
                query = query.Where(x => x.SportId == filter.SportId.Value);
            }

            return query;
        }
    }
}
