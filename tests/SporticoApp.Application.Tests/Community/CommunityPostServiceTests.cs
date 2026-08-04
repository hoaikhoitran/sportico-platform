using SporticoApp.Application.DTOs.Community;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Community;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Community;

/// <summary>
/// Covers community post creation/authorization, visibility rules (public feed vs. author's own
/// draft/hidden posts), like/unlike idempotency, and the recruitment-post application lifecycle
/// (apply → accept/reject → auto-close when full → participant leaving reopens the post).
/// </summary>
public class CommunityPostServiceTests
{
    private static readonly Guid AuthorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Harness
    {
        public CommunityPostService Svc = null!;
        public FakeCommunityPostRepository Posts = null!;
        public FakeCommunityPostReactionRepository Reactions = null!;
        public FakeCommunityPostApplicationRepository Applications = null!;
        public FakeCommunityUserRepository Users = null!;
    }

    private static Harness Build()
    {
        var posts = new FakeCommunityPostRepository();
        var reactions = new FakeCommunityPostReactionRepository();
        var applications = new FakeCommunityPostApplicationRepository();
        var users = new FakeCommunityUserRepository();
        users.Add(AuthorId);
        users.Add(OtherUserId);

        var svc = new CommunityPostService(
            posts,
            reactions,
            applications,
            users,
            new FakeCommunitySportRepository(),
            new FakeCommunityNotificationRepository(),
            new CreateCommunityPostRequestValidator(),
            new UpdateCommunityPostRequestValidator(),
            new CommunityPostFilterRequestValidator(),
            new CreateApplicationRequestValidator(),
            new CommunityApplicationFilterRequestValidator());

        return new Harness { Svc = svc, Posts = posts, Reactions = reactions, Applications = applications, Users = users };
    }

    private static CreateCommunityPostRequest RecruitmentRequest(int maxParticipants = 3) => new()
    {
        PostType = CommunityPostTypes.LookingForPlayers,
        Title = "Cần thêm người đá bóng",
        Content = "5h chiều thứ 7 tại sân ABC",
        SportId = 1,
        StartAt = DateTime.UtcNow.AddDays(2),
        MaxParticipants = maxParticipants
    };

    // 1. Active user creates a post.
    [Fact]
    public async Task Create_ActiveUser_Succeeds_AndAcceptedParticipantsStartsAtOne()
    {
        var h = Build();

        var result = await h.Svc.CreateAsync(AuthorId, RecruitmentRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(CommunityPostStatuses.Published, result.Data!.Status);
        Assert.Equal(1, result.Data.AcceptedParticipants); // the author counts as the first participant
    }

    // 2. Inactive/banned user cannot create a post.
    [Fact]
    public async Task Create_InactiveUser_ThrowsForbidden()
    {
        var h = Build();
        h.Users.Add(AuthorId, status: UserStatuses.Banned);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => h.Svc.CreateAsync(AuthorId, RecruitmentRequest()));
        Assert.Equal(ErrorCodes.AccountNotActive, ex.Code);
    }

    // 3. Recruitment post types require SportId/StartAt/MaxParticipants.
    [Fact]
    public async Task Create_LookingForPlayers_MissingRequiredFields_ThrowsValidation()
    {
        var h = Build();
        var request = new CreateCommunityPostRequest
        {
            PostType = CommunityPostTypes.LookingForPlayers,
            Title = "Cần người",
            Content = "..."
            // SportId / StartAt / MaxParticipants all missing
        };

        await Assert.ThrowsAsync<SporticoApp.Shared.Exceptions.ValidationException>(() => h.Svc.CreateAsync(AuthorId, request));
    }

    // 4/5. Update own post succeeds; another user cannot update it.
    [Fact]
    public async Task Update_Owner_Succeeds()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;

        var result = await h.Svc.UpdateAsync(AuthorId, created.Id, new UpdateCommunityPostRequest { Title = "Updated title" });

        Assert.Equal("Updated title", result.Data!.Title);
    }

    [Fact]
    public async Task Update_NonOwner_ThrowsForbidden()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            h.Svc.UpdateAsync(OtherUserId, created.Id, new UpdateCommunityPostRequest { Title = "Hacked" }));

        Assert.Equal(ErrorCodes.CommunityPostNotOwned, ex.Code);
    }

    // 6. Soft delete.
    [Fact]
    public async Task Delete_Owner_SoftDeletes_KeepsRecord()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;

        await h.Svc.DeleteAsync(AuthorId, created.Id);

        var stored = h.Posts.Posts[created.Id];
        Assert.Equal(CommunityPostStatuses.Deleted, stored.Status);
        Assert.NotNull(stored.DeletedAt);
        Assert.True(h.Posts.Posts.ContainsKey(created.Id)); // record kept, not hard-deleted
    }

    // 7/8. Public feed excludes hidden/deleted; the author can still fetch their own hidden post by id.
    [Fact]
    public async Task GetFeed_ExcludesDeletedAndHiddenPosts()
    {
        var h = Build();
        var published = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;
        var toHide = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;
        h.Posts.Posts[toHide.Id].Status = CommunityPostStatuses.Hidden;

        var feed = await h.Svc.GetFeedAsync(null, new CommunityPostFilterRequest());

        Assert.Contains(feed.Data!.Items, p => p.Id == published.Id);
        Assert.DoesNotContain(feed.Data.Items, p => p.Id == toHide.Id);
    }

    [Fact]
    public async Task GetById_HiddenPost_NonOwnerGetsNotFound_OwnerCanStillSeeIt()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;
        h.Posts.Posts[created.Id].Status = CommunityPostStatuses.Hidden;

        await Assert.ThrowsAsync<NotFoundException>(() => h.Svc.GetByIdAsync(OtherUserId, created.Id));

        var ownerView = await h.Svc.GetByIdAsync(AuthorId, created.Id);
        Assert.True(ownerView.IsSuccess);
    }

    // Like/unlike idempotency.
    [Fact]
    public async Task Like_CalledTwice_IsIdempotent_CountsOnlyOnce()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;

        await h.Svc.LikeAsync(OtherUserId, created.Id);
        await h.Svc.LikeAsync(OtherUserId, created.Id);

        Assert.Equal(1, h.Posts.Posts[created.Id].ReactionCount);
    }

    [Fact]
    public async Task Unlike_CalledTwice_IsIdempotent_NeverGoesNegative()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;
        await h.Svc.LikeAsync(OtherUserId, created.Id);

        await h.Svc.UnlikeAsync(OtherUserId, created.Id);
        await h.Svc.UnlikeAsync(OtherUserId, created.Id);

        Assert.Equal(0, h.Posts.Posts[created.Id].ReactionCount);
    }

    // Applications: cannot apply to own post; cannot apply to a full post; accept/reject; auto-close;
    // and a departing accepted participant frees the slot again.
    [Fact]
    public async Task Apply_ToOwnPost_ThrowsForbidden()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest())).Data!;

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            h.Svc.ApplyAsync(AuthorId, created.Id, new CreateApplicationRequest()));
        Assert.Equal(ErrorCodes.CommunityApplicationNotAllowed, ex.Code);
    }

    // Accepting the last applicant auto-closes the post (see Accept_And_PostFillsUp_AutoCloses), so
    // the AcceptedParticipants>=MaxParticipants guard inside ApplyAsync is a defensive check for a
    // full-but-still-published state that shouldn't normally occur — exercise it directly.
    [Fact]
    public async Task Apply_ToFullPost_ThrowsConflict()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest(maxParticipants: 2))).Data!;
        h.Posts.Posts[created.Id].AcceptedParticipants = 2; // simulate "full" while still Published

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.ApplyAsync(OtherUserId, created.Id, new CreateApplicationRequest()));
        Assert.Equal(ErrorCodes.CommunityPostFull, ex.Code);
    }

    // The realistic path: once a post auto-closes from being filled, a further application is
    // rejected because the post is no longer published — not because of the (now unreachable in
    // practice) full-count guard above.
    [Fact]
    public async Task Apply_ToAutoClosedFullPost_ThrowsNotPublished()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest(maxParticipants: 2))).Data!;
        var firstApplicant = Guid.NewGuid();
        h.Users.Add(firstApplicant);
        var application = (await h.Svc.ApplyAsync(firstApplicant, created.Id, new CreateApplicationRequest())).Data!;
        await h.Svc.AcceptApplicationAsync(AuthorId, application.Id); // fills + auto-closes

        var secondApplicant = Guid.NewGuid();
        h.Users.Add(secondApplicant);
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.ApplyAsync(secondApplicant, created.Id, new CreateApplicationRequest()));
        Assert.Equal(ErrorCodes.CommunityPostNotPublished, ex.Code);
    }

    [Fact]
    public async Task Apply_ToExpiredPost_ThrowsConflict()
    {
        var h = Build();
        var request = RecruitmentRequest();
        request.StartAt = DateTime.UtcNow.AddMinutes(-10); // already started
        var created = (await h.Svc.CreateAsync(AuthorId, request)).Data!;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.ApplyAsync(OtherUserId, created.Id, new CreateApplicationRequest()));
        Assert.Equal(ErrorCodes.CommunityPostExpired, ex.Code);
    }

    [Fact]
    public async Task Accept_And_PostFillsUp_AutoCloses()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest(maxParticipants: 2))).Data!; // room for 1 more
        var applicant = Guid.NewGuid();
        h.Users.Add(applicant);
        var application = (await h.Svc.ApplyAsync(applicant, created.Id, new CreateApplicationRequest())).Data!;

        var accepted = await h.Svc.AcceptApplicationAsync(AuthorId, application.Id);

        Assert.Equal(CommunityApplicationStatuses.Accepted, accepted.Data!.Status);
        var post = h.Posts.Posts[created.Id];
        Assert.Equal(2, post.AcceptedParticipants);
        Assert.Equal(CommunityPostStatuses.Closed, post.Status); // full → auto-closed
    }

    [Fact]
    public async Task Reject_Application_LeavesSlotOpen()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest(maxParticipants: 2))).Data!;
        var applicant = Guid.NewGuid();
        h.Users.Add(applicant);
        var application = (await h.Svc.ApplyAsync(applicant, created.Id, new CreateApplicationRequest())).Data!;

        var rejected = await h.Svc.RejectApplicationAsync(AuthorId, application.Id);

        Assert.Equal(CommunityApplicationStatuses.Rejected, rejected.Data!.Status);
        Assert.Equal(1, h.Posts.Posts[created.Id].AcceptedParticipants); // unchanged
    }

    [Fact]
    public async Task Concurrent_AcceptTwoApplicationsForLastSlot_SecondThrowsPostFull()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest(maxParticipants: 2))).Data!; // only 1 free slot
        var applicant1 = Guid.NewGuid();
        var applicant2 = Guid.NewGuid();
        h.Users.Add(applicant1);
        h.Users.Add(applicant2);
        var app1 = (await h.Svc.ApplyAsync(applicant1, created.Id, new CreateApplicationRequest())).Data!;
        var app2 = (await h.Svc.ApplyAsync(applicant2, created.Id, new CreateApplicationRequest())).Data!;

        await h.Svc.AcceptApplicationAsync(AuthorId, app1.Id); // fills the post, auto-closes

        var ex = await Assert.ThrowsAsync<ConflictException>(() => h.Svc.AcceptApplicationAsync(AuthorId, app2.Id));
        Assert.Equal(ErrorCodes.CommunityPostFull, ex.Code);
    }

    [Fact]
    public async Task CancelMyApplication_AfterAccepted_DecrementsCounter_AndReopensClosedPost()
    {
        var h = Build();
        var created = (await h.Svc.CreateAsync(AuthorId, RecruitmentRequest(maxParticipants: 2))).Data!;
        var applicant = Guid.NewGuid();
        h.Users.Add(applicant);
        var application = (await h.Svc.ApplyAsync(applicant, created.Id, new CreateApplicationRequest())).Data!;
        await h.Svc.AcceptApplicationAsync(AuthorId, application.Id); // post is now full → closed

        await h.Svc.CancelMyApplicationAsync(applicant, created.Id);

        var post = h.Posts.Posts[created.Id];
        Assert.Equal(1, post.AcceptedParticipants);
        Assert.Equal(CommunityPostStatuses.Published, post.Status); // reopened — there's room again
    }
}
