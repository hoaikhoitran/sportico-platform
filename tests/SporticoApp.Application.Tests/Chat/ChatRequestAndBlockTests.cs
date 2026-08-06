using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Chat;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Chat;

/// <summary>
/// Covers the user-to-user chat extension: opening a room between any two active users (not just
/// coach↔learner), the pending request → accept/reject gate, blocked-user restrictions, and
/// attachment-only messages. Existing coach↔learner chat behavior (no booking relationship needed to
/// open, message send/list) is covered by <see cref="ChatServiceMessagesTests"/> and is unaffected.
/// </summary>
public class ChatRequestAndBlockTests
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class FakeChatRepo : IChatRepository
    {
        public readonly Dictionary<Guid, ChatRoom> Rooms = new();
        public readonly List<Message> Messages = new();

        public Task<ChatRoom?> GetRoomByIdAsync(Guid roomId) => Task.FromResult(Rooms.TryGetValue(roomId, out var r) ? r : null);
        public Task<ChatRoom?> GetRoomByIdForUpdateAsync(Guid roomId) => GetRoomByIdAsync(roomId);

        public Task<ChatRoom?> GetRoomByUsersAsync(Guid userId1, Guid userId2)
        {
            var u1 = userId1.CompareTo(userId2) <= 0 ? userId1 : userId2;
            var u2 = userId1.CompareTo(userId2) <= 0 ? userId2 : userId1;
            return Task.FromResult(Rooms.Values.FirstOrDefault(r => r.User1Id == u1 && r.User2Id == u2));
        }

        public Task<List<ChatRoom>> GetRoomsForUserAsync(Guid userId)
            => Task.FromResult(Rooms.Values.Where(r => r.User1Id == userId || r.User2Id == userId).ToList());

        public Task<(List<Message> Items, int TotalCount)> GetMessagesByRoomAsync(Guid roomId, ChatMessageFilterRequest filter)
        {
            var items = Messages.Where(m => m.RoomId == roomId).ToList();
            return Task.FromResult((items, items.Count));
        }

        public Task<ChatRoom> AddRoomAsync(ChatRoom room)
        {
            Rooms[room.Id] = room;
            return Task.FromResult(room);
        }

        public Task AddMessageAsync(Message message) { Messages.Add(message); return Task.CompletedTask; }
        public Task AddMessageWithoutSaveAsync(Message message) { Messages.Add(message); return Task.CompletedTask; }
        public Task AddAttachmentsWithoutSaveAsync(IEnumerable<MessageAttachment> attachments) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserRepo : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult<User?>(new User { Id = id, Status = UserStatuses.Active, FullName = "U", Email = id + "@x.com" });
        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> GetByEmailWithRolesAsync(string email) => throw new NotImplementedException();
        public Task AddAsync(User user) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(User user) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
        public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByIdWithRolesAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(SporticoApp.Application.DTOs.Users.AdminUserFilterRequest filter) => throw new NotImplementedException();
        public Task<User?> GetByIdForAdminUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmailAsync(string email) => throw new NotImplementedException();
    }

    private sealed class FakeBookingRepo : IBookingRepository
    {
        public Booking? Relationship;
        public Task<Booking?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdWithTrainingPackageAsync(Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForLearnerForUpdateAsync(Guid learnerId, Guid id) => throw new NotImplementedException();
        public Task<Booking?> GetByIdForCoachForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByLearnerAsync(Guid learnerId, BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<(List<Booking> Items, int TotalCount)> GetPagedAsync(BookingFilterRequest filter) => throw new NotImplementedException();
        public Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(Guid learnerId, Guid coachId) => Task.FromResult(Relationship);
        public Task<List<Guid>> GetExpiredPendingPaymentBookingIdsAsync(DateTime nowUtc, int batchSize) => Task.FromResult(new List<Guid>());
        public Task AddAsync(Booking booking) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Booking booking) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserBlockRepo : IUserBlockRepository
    {
        public bool Blocked;
        public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId) => Task.FromResult(Blocked);
        public Task<bool> IsBlockedEitherDirectionAsync(Guid userId1, Guid userId2) => Task.FromResult(Blocked);
        public Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId) => Task.FromResult<UserBlock?>(null);
        public Task<List<UserBlock>> GetBlockedByUserAsync(Guid blockerId) => Task.FromResult(new List<UserBlock>());
        public Task AddAsync(UserBlock block) => Task.CompletedTask;
        public Task RemoveAsync(UserBlock block) => Task.CompletedTask;
    }

    private sealed class FakeNotificationRepo : INotificationRepository
    {
        public int Count;
        public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, SporticoApp.Application.DTOs.Notifications.NotificationFilterRequest filter) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotImplementedException();
        public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId) => throw new NotImplementedException();
        public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Notification notification) { Count++; return Task.CompletedTask; }
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications) { Count += notifications.Count; return Task.FromResult<Exception?>(null); }
    }

    private sealed class Harness
    {
        public ChatService Svc = null!;
        public FakeChatRepo Chat = null!;
        public FakeBookingRepo Bookings = null!;
        public FakeUserBlockRepo Blocks = null!;
        public FakeNotificationRepo Notifications = null!;
    }

    private static Harness Build()
    {
        var chat = new FakeChatRepo();
        var bookings = new FakeBookingRepo();
        var blocks = new FakeUserBlockRepo();
        var notifications = new FakeNotificationRepo();

        var svc = new ChatService(
            chat,
            new FakeUserRepo(),
            bookings,
            blocks,
            notifications,
            new CreateChatRoomRequestValidator(),
            new ChatMessageFilterRequestValidator(),
            new SendMessageRequestValidator());

        return new Harness { Svc = svc, Chat = chat, Bookings = bookings, Blocks = blocks, Notifications = notifications };
    }

    // Learner ↔ learner (no coach involved at all) — the new user-to-user capability.
    [Fact]
    public async Task CreateRoom_BetweenTwoNonCoachUsers_Succeeds_StartsPending()
    {
        var h = Build();

        var result = await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB });

        Assert.True(result.IsSuccess);
        Assert.Equal(ChatRoomStatuses.Pending, result.Data!.Status);
        Assert.Equal(UserA, result.Data.RequestedByUserId);
    }

    [Fact]
    public async Task CreateRoom_WithExistingBookingRelationship_StartsActive_NoPendingGate()
    {
        var h = Build();
        h.Bookings.Relationship = new Booking { Id = Guid.NewGuid(), Status = BookingStatuses.Active };

        var result = await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB });

        Assert.Equal(ChatRoomStatuses.Active, result.Data!.Status);
    }

    [Fact]
    public async Task CreateRoom_WithSelf_ThrowsForbidden()
    {
        var h = Build();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserA }));
        Assert.Equal(ErrorCodes.ChatCannotMessageSelf, ex.Code);
    }

    [Fact]
    public async Task CreateRoom_Twice_ReturnsSameExistingRoom_NoDuplicate()
    {
        var h = Build();
        var first = await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB });

        var second = await h.Svc.CreateOrGetRoomAsync(UserB, new CreateChatRoomRequest { TargetUserId = UserA });

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(h.Chat.Rooms);
    }

    [Fact]
    public async Task CreateRoom_WhenBlocked_ThrowsForbidden()
    {
        var h = Build();
        h.Blocks.Blocked = true;

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB }));
        Assert.Equal(ErrorCodes.ChatUserBlocked, ex.Code);
    }

    [Fact]
    public async Task AcceptRoom_ByRecipient_TransitionsToActive()
    {
        var h = Build();
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;

        var accepted = await h.Svc.AcceptRoomAsync(UserB, room.Id);

        Assert.Equal(ChatRoomStatuses.Active, accepted.Data!.Status);
    }

    [Fact]
    public async Task AcceptRoom_ByRequesterThemselves_ThrowsForbidden()
    {
        var h = Build();
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;

        await Assert.ThrowsAsync<ForbiddenException>(() => h.Svc.AcceptRoomAsync(UserA, room.Id));
    }

    [Fact]
    public async Task RejectRoom_ByRecipient_TransitionsToRejected_AndMessagingIsBlocked()
    {
        var h = Build();
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;

        await h.Svc.RejectRoomAsync(UserB, room.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.SendMessageAsync(UserA, room.Id, new SendMessageRequest { Content = "hi" }));
        Assert.Equal(ErrorCodes.ChatRoomRejected, ex.Code);
    }

    // While pending, the recipient must accept before they can reply.
    [Fact]
    public async Task SendMessage_WhilePending_RecipientCannotReplyUntilAccepted()
    {
        var h = Build();
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Svc.SendMessageAsync(UserB, room.Id, new SendMessageRequest { Content = "hi" }));
        Assert.Equal(ErrorCodes.ChatRoomNotPending, ex.Code);

        // The requester, meanwhile, can keep sending while pending.
        var sent = await h.Svc.SendMessageAsync(UserA, room.Id, new SendMessageRequest { Content = "hello?" });
        Assert.True(sent.IsSuccess);
    }

    [Fact]
    public async Task SendMessage_WhenBlocked_ThrowsForbidden()
    {
        var h = Build();
        h.Bookings.Relationship = new Booking { Id = Guid.NewGuid(), Status = BookingStatuses.Active }; // active room
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;
        h.Blocks.Blocked = true;

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            h.Svc.SendMessageAsync(UserA, room.Id, new SendMessageRequest { Content = "hi" }));
        Assert.Equal(ErrorCodes.ChatUserBlocked, ex.Code);
    }

    [Fact]
    public async Task SendMessage_TextOnly_Succeeds_NotifiesReceiverOnly()
    {
        var h = Build();
        h.Bookings.Relationship = new Booking { Id = Guid.NewGuid(), Status = BookingStatuses.Active };
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;
        h.Notifications.Count = 0; // reset after room-creation notification

        var result = await h.Svc.SendMessageAsync(UserA, room.Id, new SendMessageRequest { Content = "Hello!" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello!", result.Data!.Content);
        Assert.Equal(1, h.Notifications.Count); // exactly one notification, to the receiver
    }

    [Fact]
    public async Task SendMessage_AttachmentOnly_NoContent_Succeeds()
    {
        var h = Build();
        h.Bookings.Relationship = new Booking { Id = Guid.NewGuid(), Status = BookingStatuses.Active };
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;

        var result = await h.Svc.SendMessageAsync(UserA, room.Id, new SendMessageRequest
        {
            Attachments = new List<SendMessageAttachmentRequest> { new() { FileUrl = "https://cdn.example.com/a.png", FileType = "image" } }
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Data!.Content);
        Assert.Single(result.Data.Attachments);
    }

    [Fact]
    public async Task SendMessage_NoContentNoAttachment_ThrowsValidation()
    {
        var h = Build();
        h.Bookings.Relationship = new Booking { Id = Guid.NewGuid(), Status = BookingStatuses.Active };
        var room = (await h.Svc.CreateOrGetRoomAsync(UserA, new CreateChatRoomRequest { TargetUserId = UserB })).Data!;

        await Assert.ThrowsAsync<SporticoApp.Shared.Exceptions.ValidationException>(() =>
            h.Svc.SendMessageAsync(UserA, room.Id, new SendMessageRequest()));
    }
}
