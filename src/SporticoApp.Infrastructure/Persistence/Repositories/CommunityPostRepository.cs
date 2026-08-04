using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class CommunityPostRepository : ICommunityPostRepository
    {
        private readonly AppDbContext _context;

        public CommunityPostRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CommunityPost?> GetByIdAsync(Guid id)
        {
            return await _context.CommunityPosts
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Sport)
                .Include(x => x.Media.OrderBy(m => m.OrderIndex))
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<CommunityPost?> GetByIdForUpdateAsync(Guid id)
        {
            return await _context.CommunityPosts
                .Include(x => x.Media)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<(List<CommunityPost> Items, int TotalCount)> GetPagedAsync(
            CommunityPostFilterRequest filter, Guid? currentUserId)
        {
            IQueryable<CommunityPost> query = _context.CommunityPosts
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Sport)
                .Include(x => x.Media.OrderBy(m => m.OrderIndex))
                .Where(x => CommunityPostStatuses.PubliclyVisible.Contains(x.Status));

            query = ApplyCommonFilter(query, filter);

            if (filter.FollowingOnly && currentUserId.HasValue)
            {
                var followingIds = _context.Follows
                    .Where(f => f.FollowerId == currentUserId.Value)
                    .Select(f => f.FollowingId);
                query = query.Where(x => followingIds.Contains(x.AuthorId));
            }

            var totalCount = await query.CountAsync();

            query = ApplySort(query, filter.SortBy);

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<CommunityPost> Items, int TotalCount)> GetPagedByAuthorAsync(
            Guid authorId, CommunityPostFilterRequest filter)
        {
            IQueryable<CommunityPost> query = _context.CommunityPosts
                .AsNoTracking()
                .Include(x => x.Sport)
                .Include(x => x.Media.OrderBy(m => m.OrderIndex))
                .Where(x => x.AuthorId == authorId && x.Status != CommunityPostStatuses.Deleted);

            query = ApplyCommonFilter(query, filter);

            var totalCount = await query.CountAsync();
            query = ApplySort(query, filter.SortBy);

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<CommunityPost> Items, int TotalCount)> GetPagedForAdminAsync(AdminCommunityPostFilterRequest filter)
        {
            IQueryable<CommunityPost> query = _context.CommunityPosts
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Sport);

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            if (!string.IsNullOrWhiteSpace(filter.PostType))
            {
                query = query.Where(x => x.PostType == filter.PostType);
            }

            if (filter.SportId.HasValue)
            {
                query = query.Where(x => x.SportId == filter.SportId.Value);
            }

            if (filter.AuthorId.HasValue)
            {
                query = query.Where(x => x.AuthorId == filter.AuthorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                query = query.Where(x => EF.Functions.ILike(x.Title, $"%{keyword}%") || EF.Functions.ILike(x.Content, $"%{keyword}%"));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);
            }

            if (filter.ReportedOnly)
            {
                var reportedPostIds = _context.Reports
                    .Where(r =>
                        r.TargetType == ReportTargetTypes.CommunityPost &&
                        (r.Status == ReportStatuses.Pending || r.Status == ReportStatuses.Reviewing) &&
                        r.TargetId != null)
                    .Select(r => r.TargetId!.Value);
                query = query.Where(x => reportedPostIds.Contains(x.Id));
            }

            var totalCount = await query.CountAsync();

            query = filter.SortBy == "most_discussed"
                ? query.OrderByDescending(x => x.CommentCount + x.ReactionCount)
                : query.OrderByDescending(x => x.CreatedAt);

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<CommunityPost>> GetExpiryCandidatesAsync(DateTime nowUtc, int batchSize)
        {
            return await _context.CommunityPosts
                .Where(x =>
                    (x.Status == CommunityPostStatuses.Published || x.Status == CommunityPostStatuses.Closed) &&
                    ((x.EndAt != null && x.EndAt < nowUtc) ||
                     (x.EndAt == null && x.StartAt != null && x.StartAt < nowUtc.AddDays(-1))))
                .OrderBy(x => x.StartAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public Task AddWithoutSaveAsync(CommunityPost post)
        {
            _context.CommunityPosts.Add(post);
            return Task.CompletedTask;
        }

        public async Task<int> IncrementViewCountAsync(Guid postId)
        {
            return await _context.CommunityPosts
                .Where(x => x.Id == postId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ViewCount, x => x.ViewCount + 1));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static IQueryable<CommunityPost> ApplyCommonFilter(IQueryable<CommunityPost> query, CommunityPostFilterRequest filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.PostType))
            {
                query = query.Where(x => x.PostType == filter.PostType);
            }

            if (filter.SportId.HasValue)
            {
                query = query.Where(x => x.SportId == filter.SportId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();
                query = query.Where(x => EF.Functions.ILike(x.Title, $"%{keyword}%") || EF.Functions.ILike(x.Content, $"%{keyword}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                var city = filter.City.Trim();
                query = query.Where(x => x.LocationName != null && EF.Functions.ILike(x.LocationName, $"%{city}%"));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(x => x.StartAt == null || x.StartAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(x => x.StartAt == null || x.StartAt <= filter.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Level))
            {
                query = query.Where(x => x.Level == filter.Level);
            }

            if (filter.HasAvailableSlots == true)
            {
                query = query.Where(x => x.MaxParticipants != null && x.AcceptedParticipants < x.MaxParticipants.Value);
            }

            if (filter.AuthorId.HasValue)
            {
                query = query.Where(x => x.AuthorId == filter.AuthorId.Value);
            }

            return query;
        }

        private static IQueryable<CommunityPost> ApplySort(IQueryable<CommunityPost> query, string sortBy) => sortBy switch
        {
            "upcoming" => query.OrderBy(x => x.StartAt == null).ThenBy(x => x.StartAt),
            "most_discussed" => query.OrderByDescending(x => x.CommentCount + x.ReactionCount),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
    }
}
