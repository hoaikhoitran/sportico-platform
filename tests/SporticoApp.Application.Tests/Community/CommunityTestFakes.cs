using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Tests.Community;

internal sealed class FakeCommunityPostRepository : ICommunityPostRepository
{
    public readonly Dictionary<Guid, CommunityPost> Posts = new();
    public int SaveCount;

    public Task<CommunityPost?> GetByIdAsync(Guid id) => Task.FromResult(Posts.TryGetValue(id, out var p) ? p : null);
    public Task<CommunityPost?> GetByIdForUpdateAsync(Guid id) => GetByIdAsync(id);

    public Task<(List<CommunityPost> Items, int TotalCount)> GetPagedAsync(CommunityPostFilterRequest filter, Guid? currentUserId)
    {
        var items = Posts.Values.Where(p => CommunityPostStatuses.PubliclyVisible.Contains(p.Status)).ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task<(List<CommunityPost> Items, int TotalCount)> GetPagedByAuthorAsync(Guid authorId, CommunityPostFilterRequest filter)
    {
        var items = Posts.Values.Where(p => p.AuthorId == authorId && p.Status != CommunityPostStatuses.Deleted).ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task<(List<CommunityPost> Items, int TotalCount)> GetPagedForAdminAsync(AdminCommunityPostFilterRequest filter)
    {
        var items = Posts.Values.ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task<List<CommunityPost>> GetExpiryCandidatesAsync(DateTime nowUtc, int batchSize)
    {
        var items = Posts.Values
            .Where(p => (p.Status == CommunityPostStatuses.Published || p.Status == CommunityPostStatuses.Closed) &&
                        p.EndAt != null && p.EndAt < nowUtc)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddWithoutSaveAsync(CommunityPost post)
    {
        Posts[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task<int> IncrementViewCountAsync(Guid postId)
    {
        if (Posts.TryGetValue(postId, out var p)) p.ViewCount++;
        return Task.FromResult(1);
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeCommunityCommentRepository : ICommunityCommentRepository
{
    public readonly Dictionary<Guid, CommunityComment> Comments = new();
    public int SaveCount;

    public Task<CommunityComment?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(Comments.TryGetValue(id, out var c) ? c : null);

    public Task<(List<CommunityComment> Items, int TotalCount)> GetRootCommentsPagedAsync(Guid postId, CommunityCommentFilterRequest filter)
    {
        var items = Comments.Values
            .Where(c => c.PostId == postId && c.ParentCommentId == null && c.Status != CommunityCommentStatuses.Deleted)
            .ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task<(List<CommunityComment> Items, int TotalCount)> GetForAdminPagedAsync(Guid postId, CommunityCommentFilterRequest filter)
    {
        var items = Comments.Values.Where(c => c.PostId == postId).ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task AddWithoutSaveAsync(CommunityComment comment)
    {
        Comments[comment.Id] = comment;
        if (comment.ParentCommentId.HasValue && Comments.TryGetValue(comment.ParentCommentId.Value, out var parent))
        {
            parent.Replies.Add(comment);
        }
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeCommunityPostReactionRepository : ICommunityPostReactionRepository
{
    public readonly List<CommunityPostReaction> Reactions = new();

    public Task<CommunityPostReaction?> GetAsync(Guid postId, Guid userId)
        => Task.FromResult(Reactions.FirstOrDefault(r => r.PostId == postId && r.UserId == userId));

    public Task AddWithoutSaveAsync(CommunityPostReaction reaction)
    {
        Reactions.Add(reaction);
        return Task.CompletedTask;
    }

    public Task RemoveWithoutSaveAsync(CommunityPostReaction reaction)
    {
        Reactions.RemoveAll(r => r.PostId == reaction.PostId && r.UserId == reaction.UserId);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}

internal sealed class FakeCommunityPostApplicationRepository : ICommunityPostApplicationRepository
{
    public readonly List<CommunityPostApplication> Applications = new();

    public Task<CommunityPostApplication?> GetByIdForUpdateAsync(Guid id)
        => Task.FromResult(Applications.FirstOrDefault(a => a.Id == id));

    public Task<CommunityPostApplication?> GetByPostAndApplicantAsync(Guid postId, Guid applicantId)
        => Task.FromResult(Applications.FirstOrDefault(a => a.PostId == postId && a.ApplicantId == applicantId));

    public Task<(List<CommunityPostApplication> Items, int TotalCount)> GetPagedByPostAsync(Guid postId, CommunityApplicationFilterRequest filter)
    {
        var items = Applications.Where(a => a.PostId == postId).ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task AddWithoutSaveAsync(CommunityPostApplication application)
    {
        Applications.Add(application);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}

internal sealed class FakeCommunityReportRepository : ICommunityReportRepository
{
    public readonly List<Report> Reports = new();

    public Task<Report?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(Reports.FirstOrDefault(r => r.Id == id));

    public Task<Report?> GetOpenReportAsync(string targetType, Guid targetId, Guid reporterId)
        => Task.FromResult(Reports.FirstOrDefault(r =>
            r.TargetType == targetType && r.TargetId == targetId && r.reporter_id == reporterId &&
            (r.Status == ReportStatuses.Pending || r.Status == ReportStatuses.Reviewing)));

    public Task<int> CountOpenByTargetAsync(string targetType, Guid targetId)
        => Task.FromResult(Reports.Count(r =>
            r.TargetType == targetType && r.TargetId == targetId &&
            (r.Status == ReportStatuses.Pending || r.Status == ReportStatuses.Reviewing)));

    public Task<(List<Report> Items, int TotalCount)> GetPagedAsync(AdminReportFilterRequest filter)
    {
        var items = Reports.ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task AddWithoutSaveAsync(Report report)
    {
        Reports.Add(report);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
}

internal sealed class FakeCommunityUserRepository : IUserRepository
{
    public readonly Dictionary<Guid, User> Users = new();

    public User Add(Guid id, string status = "active")
    {
        var u = new User { Id = id, Status = status, FullName = "Test User " + id, Email = id + "@example.com" };
        Users[id] = u;
        return u;
    }

    public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
    public Task<User?> GetByEmailWithRolesAsync(string email) => throw new NotImplementedException();
    public Task AddAsync(User user) => throw new NotImplementedException();
    public Task AddWithoutSaveAsync(User user) => throw new NotImplementedException();
    public Task SaveChangesAsync() => Task.CompletedTask;
    public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
    public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
    public Task UpdateAsync(User user) => throw new NotImplementedException();

    public Task<User?> GetByIdAsync(Guid id)
        => Task.FromResult(Users.TryGetValue(id, out var u) ? u : new User { Id = id, Status = UserStatuses.Active, FullName = "Auto User", Email = id + "@example.com" });

    public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id) => throw new NotImplementedException();
    public Task<User?> GetByIdWithRolesAsync(Guid id) => throw new NotImplementedException();
    public Task<User?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
    public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(AdminUserFilterRequest filter) => throw new NotImplementedException();
    public Task<User?> GetByIdForAdminUpdateAsync(Guid id) => throw new NotImplementedException();
    public Task<bool> ExistsByEmailAsync(string email) => throw new NotImplementedException();
}

internal sealed class FakeCommunitySportRepository : ISportRepository
{
    public Task<List<int>> GetActiveSportIdsAsync(List<int> sportIds) => Task.FromResult(sportIds);
    public Task<bool> ExistsByNameAsync(string name) => Task.FromResult(false);
    public Task<bool> ExistsBySlugAsync(string slug) => Task.FromResult(false);
    public Task<Sport?> GetByIdAsync(int id) => Task.FromResult<Sport?>(new Sport { Id = id, Name = "Football" });
    public Task AddAsync(Sport sport) => Task.CompletedTask;
}

internal sealed class FakeCommunityNotificationRepository : INotificationRepository
{
    public int Count;

    public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, NotificationFilterRequest filter) => throw new NotImplementedException();
    public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotImplementedException();
    public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId) => throw new NotImplementedException();
    public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId) => throw new NotImplementedException();
    public Task AddWithoutSaveAsync(Notification notification) { Count++; return Task.CompletedTask; }
    public Task SaveChangesAsync() => Task.CompletedTask;
    public Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications)
    {
        Count += notifications.Count;
        return Task.FromResult<Exception?>(null);
    }
}
