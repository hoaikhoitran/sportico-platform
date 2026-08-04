using FluentValidation;
using SporticoApp.Application.DTOs.Bookings;
using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Chat;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Enums;
using SporticoApp.Shared.Exceptions;
using Xunit;
using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

namespace SporticoApp.Application.Tests.Chat;

/// <summary>
/// Regression for the reported HTTP 500 on
/// GET /api/chat/rooms/{roomId}/messages. Every expected business case must
/// surface as a typed AppException (mapped to 4xx by the middleware) or a
/// successful PagedResult — never an unhandled exception (500).
/// </summary>
public class ChatServiceMessagesTests
{
    private static readonly Guid ParticipantId = Guid.Parse("22610368-14f6-4ff6-865d-37714524f8ed");
    private static readonly Guid OtherUserId = Guid.Parse("9f3c035a-2084-4e4f-b035-1ec7a922755d");
    private static readonly Guid RoomId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static ChatService CreateService(ChatRoom? room, List<Message>? messages = null)
        => new(
            new FakeChatRepository(room, messages ?? new List<Message>()),
            new FakeUserRepository(),
            new FakeBookingRepository(),
            new FakeUserBlockRepository(),
            new FakeNotificationRepository(),
            new CreateChatRoomRequestValidator(),
            new ChatMessageFilterRequestValidator(),
            new SendMessageRequestValidator());

    [Fact]
    public async Task GetMessagesAsync_ValidParticipantEmptyRoom_ReturnsSuccessfulPagedResult()
    {
        var room = new ChatRoom { Id = RoomId, User1Id = ParticipantId, User2Id = OtherUserId };
        var service = CreateService(room);

        var result = await service.GetMessagesAsync(
            ParticipantId, RoomId, new ChatMessageFilterRequest { PageNumber = 1, PageSize = 5 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!.Items);
        Assert.Equal(1, result.Data.PageNumber);
        Assert.Equal(5, result.Data.PageSize);
        Assert.Equal(0, result.Data.TotalCount);
        Assert.Equal(0, result.Data.TotalPages);
        Assert.False(result.Data.HasPrevious);
        Assert.False(result.Data.HasNext);
    }

    [Fact]
    public async Task GetMessagesAsync_DefaultPaging_UsesSafeDefaults()
    {
        var room = new ChatRoom { Id = RoomId, User1Id = ParticipantId, User2Id = OtherUserId };
        var service = CreateService(room);

        // No query params bound -> DTO defaults (1 / 20).
        var result = await service.GetMessagesAsync(
            ParticipantId, RoomId, new ChatMessageFilterRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data!.PageNumber);
        Assert.Equal(20, result.Data.PageSize);
    }

    [Fact]
    public async Task GetMessagesAsync_MissingRoom_ThrowsNotFound()
    {
        var service = CreateService(room: null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetMessagesAsync(
                ParticipantId, RoomId, new ChatMessageFilterRequest { PageNumber = 1, PageSize = 5 }));

        Assert.Equal(ErrorType.NotFound, ex.Type);
    }

    [Fact]
    public async Task GetMessagesAsync_NonParticipant_ThrowsForbidden()
    {
        var room = new ChatRoom { Id = RoomId, User1Id = OtherUserId, User2Id = Guid.NewGuid() };
        var service = CreateService(room);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetMessagesAsync(
                ParticipantId, RoomId, new ChatMessageFilterRequest { PageNumber = 1, PageSize = 5 }));

        Assert.Equal(ErrorType.Forbidden, ex.Type);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 5)]
    [InlineData(1, 101)]
    public async Task GetMessagesAsync_InvalidPaging_ThrowsValidation(int pageNumber, int pageSize)
    {
        var room = new ChatRoom { Id = RoomId, User1Id = ParticipantId, User2Id = OtherUserId };
        var service = CreateService(room);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetMessagesAsync(
                ParticipantId, RoomId,
                new ChatMessageFilterRequest { PageNumber = pageNumber, PageSize = pageSize }));

        Assert.Equal(ErrorType.Validation, ex.Type);
    }

    internal sealed class FakeChatRepository : IChatRepository
    {
        private readonly ChatRoom? _room;
        private readonly List<Message> _messages;

        public FakeChatRepository(ChatRoom? room, List<Message> messages)
        {
            _room = room;
            _messages = messages;
        }

        public Task<ChatRoom?> GetRoomByIdAsync(Guid roomId)
            => Task.FromResult(_room is not null && _room.Id == roomId ? _room : null);

        public Task<(List<Message> Items, int TotalCount)> GetMessagesByRoomAsync(
            Guid roomId, ChatMessageFilterRequest filter)
        {
            var ordered = _messages.OrderByDescending(x => x.SentAt).ToList();
            var page = ordered
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();
            return Task.FromResult((page, ordered.Count));
        }

        public Task<ChatRoom?> GetRoomByIdForUpdateAsync(Guid roomId)
            => Task.FromResult(_room is not null && _room.Id == roomId ? _room : null);
        public Task<ChatRoom?> GetRoomByUsersAsync(Guid userId1, Guid userId2) => throw new NotImplementedException();
        public Task<List<ChatRoom>> GetRoomsForUserAsync(Guid userId) => throw new NotImplementedException();
        public Task<ChatRoom> AddRoomAsync(ChatRoom room) => throw new NotImplementedException();
        public Task AddMessageAsync(Message message) => throw new NotImplementedException();
        public Task AddMessageWithoutSaveAsync(Message message)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }
        public Task AddAttachmentsWithoutSaveAsync(IEnumerable<MessageAttachment> attachments) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    internal sealed class FakeUserRepository : IUserRepository
    {
        public User? User;

        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> GetByEmailWithRolesAsync(string email) => throw new NotImplementedException();
        public Task AddAsync(User user) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(User user) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
        public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByIdAsync(Guid id)
            => Task.FromResult(User != null && User.Id == id
                ? User
                : new User { Id = id, Status = UserStatuses.Active, FullName = "Test User", Email = "t@example.com" });
        public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedForAdminAsync(SporticoApp.Application.DTOs.Users.AdminUserFilterRequest filter) => throw new NotImplementedException();
        public Task<User?> GetByIdForAdminUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task<bool> ExistsByEmailAsync(string email) => throw new NotImplementedException();
    }

    internal sealed class FakeBookingRepository : IBookingRepository
    {
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
        public Task<Booking?> GetActiveOrCompletedBetweenUsersAsync(Guid learnerId, Guid coachId) => Task.FromResult<Booking?>(null);
        public Task<List<Guid>> GetExpiredPendingPaymentBookingIdsAsync(DateTime nowUtc, int batchSize) => Task.FromResult(new List<Guid>());
        public Task AddAsync(Booking booking) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Booking booking) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    internal sealed class FakeUserBlockRepository : IUserBlockRepository
    {
        public bool Blocked;

        public Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedUserId) => Task.FromResult(Blocked);
        public Task<bool> IsBlockedEitherDirectionAsync(Guid userId1, Guid userId2) => Task.FromResult(Blocked);
        public Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId) => Task.FromResult<UserBlock?>(null);
        public Task<List<UserBlock>> GetBlockedByUserAsync(Guid blockerId) => Task.FromResult(new List<UserBlock>());
        public Task AddAsync(UserBlock block) => Task.CompletedTask;
        public Task RemoveAsync(UserBlock block) => Task.CompletedTask;
    }

    internal sealed class FakeNotificationRepository : INotificationRepository
    {
        public Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
            Guid userId, DTOs.Notifications.NotificationFilterRequest filter) => throw new NotImplementedException();
        public Task<int> GetUnreadCountAsync(Guid userId) => throw new NotImplementedException();
        public Task<Notification?> GetByIdForUpdateAsync(Guid userId, Guid notificationId) => throw new NotImplementedException();
        public Task<List<Notification>> GetUnreadForUpdateAsync(Guid userId) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(Notification notification) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<Exception?> TryAddAndSaveAsync(IReadOnlyCollection<Notification> notifications) => Task.FromResult<Exception?>(null);
    }
}
