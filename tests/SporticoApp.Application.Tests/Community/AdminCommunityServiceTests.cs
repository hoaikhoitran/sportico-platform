using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Community;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using Xunit;

namespace SporticoApp.Application.Tests.Community;

/// <summary>
/// Covers admin moderation of community posts/comments (hide → not in public feed, restore, soft
/// delete) and reporting a post/comment into the shared Report table.
/// </summary>
public class AdminCommunityServiceTests
{
    private static readonly Guid AdminId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid AuthorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReporterId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private sealed class Harness
    {
        public AdminCommunityService AdminSvc = null!;
        public CommunityReportService ReportSvc = null!;
        public FakeCommunityPostRepository Posts = null!;
        public FakeCommunityCommentRepository Comments = null!;
        public FakeCommunityReportRepository Reports = null!;
        public CommunityPost Post = null!;
    }

    private static Harness Build()
    {
        var posts = new FakeCommunityPostRepository();
        var comments = new FakeCommunityCommentRepository();
        var reports = new FakeCommunityReportRepository();

        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = AuthorId,
            PostType = CommunityPostTypes.Discussion,
            Title = "Spam post",
            Content = "buy now",
            Status = CommunityPostStatuses.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        posts.Posts[post.Id] = post;

        var adminSvc = new AdminCommunityService(
            posts,
            comments,
            reports,
            new FakeCommunityNotificationRepository(),
            new AdminCommunityPostFilterRequestValidator(),
            new CommunityCommentFilterRequestValidator(),
            new HideContentRequestValidator(),
            new ResolveReportRequestValidator());

        var reportSvc = new CommunityReportService(
            reports, posts, comments, new CreateReportRequestValidator());

        return new Harness { AdminSvc = adminSvc, ReportSvc = reportSvc, Posts = posts, Comments = comments, Reports = reports, Post = post };
    }

    [Fact]
    public async Task HidePost_SetsHiddenStatus_WithReasonAndAdmin()
    {
        var h = Build();

        var result = await h.AdminSvc.HidePostAsync(AdminId, h.Post.Id, new HideContentRequest { Reason = "Spam" });

        Assert.Equal(CommunityPostStatuses.Hidden, result.Data!.Status);
        Assert.Equal(CommunityPostStatuses.Hidden, h.Post.Status);
        Assert.Equal(AdminId, h.Post.HiddenByUserId);
        Assert.Equal("Spam", h.Post.ModerationReason);
    }

    [Fact]
    public async Task RestorePost_AfterHide_ReturnsToPublished()
    {
        var h = Build();
        await h.AdminSvc.HidePostAsync(AdminId, h.Post.Id, new HideContentRequest { Reason = "Spam" });

        await h.AdminSvc.RestorePostAsync(AdminId, h.Post.Id);

        Assert.Equal(CommunityPostStatuses.Published, h.Post.Status);
        Assert.Null(h.Post.HiddenAt);
        Assert.Null(h.Post.ModerationReason);
    }

    [Fact]
    public async Task DeletePost_SoftDeletes_RecordKept()
    {
        var h = Build();

        await h.AdminSvc.DeletePostAsync(AdminId, h.Post.Id);

        Assert.Equal(CommunityPostStatuses.Deleted, h.Post.Status);
        Assert.NotNull(h.Post.DeletedAt);
        Assert.True(h.Posts.Posts.ContainsKey(h.Post.Id));
    }

    [Fact]
    public async Task HideComment_ThenAdminListStillSeesIt()
    {
        var h = Build();
        var comment = new CommunityComment
        {
            Id = Guid.NewGuid(),
            PostId = h.Post.Id,
            AuthorId = AuthorId,
            Content = "bad comment",
            Status = CommunityCommentStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        h.Comments.Comments[comment.Id] = comment;

        await h.AdminSvc.HideCommentAsync(AdminId, comment.Id, new HideContentRequest { Reason = "Abusive" });

        Assert.Equal(CommunityCommentStatuses.Hidden, comment.Status);
        var adminList = await h.AdminSvc.GetCommentsAsync(h.Post.Id, new CommunityCommentFilterRequest());
        Assert.Contains(adminList.Data!.Items, c => c.Id == comment.Id);
    }

    // Reporting a post creates a pending report; reporting it twice by the same user is idempotent.
    [Fact]
    public async Task ReportPost_CreatesReport_DuplicateByReporterIsIdempotent()
    {
        var h = Build();

        var first = await h.ReportSvc.CreateAsync(ReporterId, new CreateReportRequest
        {
            TargetType = ReportTargetTypes.CommunityPost,
            TargetId = h.Post.Id,
            Reason = "Spam"
        });
        var second = await h.ReportSvc.CreateAsync(ReporterId, new CreateReportRequest
        {
            TargetType = ReportTargetTypes.CommunityPost,
            TargetId = h.Post.Id,
            Reason = "Spam again"
        });

        Assert.Equal(first.Data!.Id, second.Data!.Id); // same open report returned, not a duplicate row
        Assert.Single(h.Reports.Reports);
    }

    [Fact]
    public async Task ReportComment_CreatesReport_WithCommentTargetType()
    {
        var h = Build();
        var comment = new CommunityComment
        {
            Id = Guid.NewGuid(),
            PostId = h.Post.Id,
            AuthorId = AuthorId,
            Content = "bad",
            Status = CommunityCommentStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        h.Comments.Comments[comment.Id] = comment;

        var result = await h.ReportSvc.CreateAsync(ReporterId, new CreateReportRequest
        {
            TargetType = ReportTargetTypes.CommunityComment,
            TargetId = comment.Id,
            Reason = "Abusive"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportTargetTypes.CommunityComment, result.Data!.TargetType);
    }

    // Resolving a report with ActionTaken=post_hidden actually hides the post through the same path.
    [Fact]
    public async Task ResolveReport_WithPostHiddenAction_HidesThePost()
    {
        var h = Build();
        var report = await h.ReportSvc.CreateAsync(ReporterId, new CreateReportRequest
        {
            TargetType = ReportTargetTypes.CommunityPost,
            TargetId = h.Post.Id,
            Reason = "Spam"
        });

        await h.AdminSvc.ResolveReportAsync(AdminId, report.Data!.Id, new ResolveReportRequest
        {
            Status = ReportStatuses.Resolved,
            ActionTaken = ReportActions.PostHidden,
            ResolutionNote = "Confirmed spam"
        });

        Assert.Equal(CommunityPostStatuses.Hidden, h.Post.Status);
        var stored = h.Reports.Reports.Single();
        Assert.Equal(ReportStatuses.Resolved, stored.Status);
        Assert.Equal(AdminId, stored.HandledByUserId);
    }
}
