using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Community;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Community;

/// <summary>Covers commenting, one-level-only replies, and comment ownership rules.</summary>
public class CommunityCommentServiceTests
{
    private static readonly Guid AuthorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CommenterId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Harness
    {
        public CommunityCommentService Svc = null!;
        public FakeCommunityCommentRepository Comments = null!;
        public FakeCommunityPostRepository Posts = null!;
        public CommunityPost Post = null!;
    }

    private static Harness Build()
    {
        var comments = new FakeCommunityCommentRepository();
        var posts = new FakeCommunityPostRepository();
        var users = new FakeCommunityUserRepository();
        users.Add(AuthorId);
        users.Add(CommenterId);

        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = AuthorId,
            PostType = CommunityPostTypes.Discussion,
            Title = "Discussion",
            Content = "...",
            Status = CommunityPostStatuses.Published,
            AllowComments = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        posts.Posts[post.Id] = post;

        var svc = new CommunityCommentService(
            comments,
            posts,
            users,
            new FakeCommunityNotificationRepository(),
            new CommunityCommentFilterRequestValidator(),
            new CreateCommentRequestValidator(),
            new CreateReplyRequestValidator(),
            new UpdateCommentRequestValidator());

        return new Harness { Svc = svc, Comments = comments, Posts = posts, Post = post };
    }

    [Fact]
    public async Task AddComment_Succeeds_IncrementsPostCommentCount()
    {
        var h = Build();

        var result = await h.Svc.AddCommentAsync(CommenterId, h.Post.Id, new CreateCommentRequest { Content = "Nice post!" });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, h.Post.CommentCount);
    }

    [Fact]
    public async Task AddComment_WhenCommentsDisabled_ThrowsConflict()
    {
        var h = Build();
        h.Post.AllowComments = false;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.AddCommentAsync(CommenterId, h.Post.Id, new CreateCommentRequest { Content = "Hi" }));
        Assert.Equal(ErrorCodes.CommunityCommentsDisabled, ex.Code);
    }

    [Fact]
    public async Task AddReply_ToRootComment_Succeeds_IncrementsReplyCount()
    {
        var h = Build();
        var root = (await h.Svc.AddCommentAsync(CommenterId, h.Post.Id, new CreateCommentRequest { Content = "Root" })).Data!;

        var reply = await h.Svc.AddReplyAsync(AuthorId, root.Id, new CreateReplyRequest { Content = "Reply" });

        Assert.True(reply.IsSuccess);
        Assert.Equal(1, h.Comments.Comments[root.Id].ReplyCount);
        Assert.Equal(2, h.Post.CommentCount); // root + reply
    }

    // Only one level of nesting: replying to a reply must be rejected, not silently reparented.
    [Fact]
    public async Task AddReply_ToAReply_ThrowsConflict_NestingNotAllowed()
    {
        var h = Build();
        var root = (await h.Svc.AddCommentAsync(CommenterId, h.Post.Id, new CreateCommentRequest { Content = "Root" })).Data!;
        var reply = (await h.Svc.AddReplyAsync(AuthorId, root.Id, new CreateReplyRequest { Content = "Reply" })).Data!;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.AddReplyAsync(CommenterId, reply.Id, new CreateReplyRequest { Content = "Reply to a reply" }));

        Assert.Equal(ErrorCodes.CommunityCommentNestingNotAllowed, ex.Code);
    }

    [Fact]
    public async Task UpdateComment_NonOwner_ThrowsForbidden()
    {
        var h = Build();
        var root = (await h.Svc.AddCommentAsync(CommenterId, h.Post.Id, new CreateCommentRequest { Content = "Root" })).Data!;

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            h.Svc.UpdateCommentAsync(AuthorId, root.Id, new UpdateCommentRequest { Content = "Hacked" }));
        Assert.Equal(ErrorCodes.CommunityCommentNotOwned, ex.Code);
    }

    // Deleting a comment with replies keeps the record (soft delete) and never removes the replies.
    [Fact]
    public async Task DeleteComment_WithReplies_SoftDeletes_KeepsReplies()
    {
        var h = Build();
        var root = (await h.Svc.AddCommentAsync(CommenterId, h.Post.Id, new CreateCommentRequest { Content = "Root" })).Data!;
        await h.Svc.AddReplyAsync(AuthorId, root.Id, new CreateReplyRequest { Content = "Reply" });

        await h.Svc.DeleteCommentAsync(CommenterId, root.Id);

        var stored = h.Comments.Comments[root.Id];
        Assert.Equal(CommunityCommentStatuses.Deleted, stored.Status);
        Assert.Single(stored.Replies); // reply untouched
    }
}
