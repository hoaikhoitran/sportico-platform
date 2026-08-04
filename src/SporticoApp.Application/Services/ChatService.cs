using FluentValidation;
using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserBlockRepository _userBlockRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CreateChatRoomRequest> _createRoomValidator;
        private readonly IValidator<ChatMessageFilterRequest> _filterValidator;
        private readonly IValidator<SendMessageRequest> _sendValidator;

        public ChatService(
            IChatRepository chatRepository,
            IUserRepository userRepository,
            IBookingRepository bookingRepository,
            IUserBlockRepository userBlockRepository,
            INotificationRepository notificationRepository,
            IValidator<CreateChatRoomRequest> createRoomValidator,
            IValidator<ChatMessageFilterRequest> filterValidator,
            IValidator<SendMessageRequest> sendValidator)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
            _userBlockRepository = userBlockRepository;
            _notificationRepository = notificationRepository;
            _createRoomValidator = createRoomValidator;
            _filterValidator = filterValidator;
            _sendValidator = sendValidator;
        }

        public async Task<Result<ChatRoomResponse>> CreateOrGetRoomAsync(Guid userId, CreateChatRoomRequest request)
        {
            var validationResult = await _createRoomValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var targetUserId = request.TargetUserId ?? request.CoachId!.Value;

            if (targetUserId == userId)
            {
                throw new ForbiddenException(ErrorCodes.ChatCannotMessageSelf, "You cannot open a chat room with yourself");
            }

            var target = await _userRepository.GetByIdAsync(targetUserId);
            if (target == null)
            {
                throw new NotFoundException(ErrorCodes.ChatTargetUserNotFound, "User not found");
            }

            if (target.Status != UserStatuses.Active)
            {
                throw new ConflictException(ErrorCodes.ChatTargetUserInactive, "This user is not currently active");
            }

            if (await _userBlockRepository.IsBlockedEitherDirectionAsync(userId, targetUserId))
            {
                throw new ForbiddenException(ErrorCodes.ChatUserBlocked, "You cannot start a chat with this user");
            }

            var existing = await _chatRepository.GetRoomByUsersAsync(userId, targetUserId);
            if (existing != null)
            {
                return Result<ChatRoomResponse>.Success(existing.ToResponse(userId));
            }

            var user1Id = userId.CompareTo(targetUserId) <= 0 ? userId : targetUserId;
            var user2Id = userId.CompareTo(targetUserId) <= 0 ? targetUserId : userId;

            // A pre-existing relationship (an active/completed booking between the two users) skips
            // the pending-request gate — they already know each other in a business context.
            var hasBookingRelationship =
                await _bookingRepository.GetActiveOrCompletedBetweenUsersAsync(userId, targetUserId) != null;

            var now = DateTime.UtcNow;
            var room = new ChatRoom
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = now,
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                Status = hasBookingRelationship ? ChatRoomStatuses.Active : ChatRoomStatuses.Pending,
                RequestedByUserId = userId,
                RequestedAt = now,
                AcceptedAt = hasBookingRelationship ? now : null
            };

            var saved = await _chatRepository.AddRoomAsync(room);

            if (saved.Id == room.Id) // only notify if this call actually created the room
            {
                await _notificationRepository.TryAddAndSaveAsync(new[]
                {
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = targetUserId,
                        Title = hasBookingRelationship ? "New chat" : "New chat request",
                        Content = hasBookingRelationship
                            ? "You have a new conversation"
                            : "Someone wants to start a conversation with you",
                        Type = NotificationTypeConstants.Message,
                        CreatedAt = DateTime.UtcNow
                    }
                });
            }

            return Result<ChatRoomResponse>.Success(saved.ToResponse(userId));
        }

        public async Task<Result<List<ChatRoomResponse>>> GetRoomsAsync(Guid userId)
        {
            var rooms = await _chatRepository.GetRoomsForUserAsync(userId);
            var response = rooms.Select(x => x.ToResponse(userId)).ToList();
            return Result<List<ChatRoomResponse>>.Success(response);
        }

        public async Task<Result<ChatRoomResponse>> AcceptRoomAsync(Guid userId, Guid roomId)
        {
            var room = await ChangeRequestStatusAsync(userId, roomId, accept: true);
            return Result<ChatRoomResponse>.Success(room.ToResponse(userId));
        }

        public async Task<Result<ChatRoomResponse>> RejectRoomAsync(Guid userId, Guid roomId)
        {
            var room = await ChangeRequestStatusAsync(userId, roomId, accept: false);
            return Result<ChatRoomResponse>.Success(room.ToResponse(userId));
        }

        private async Task<ChatRoom> ChangeRequestStatusAsync(Guid userId, Guid roomId, bool accept)
        {
            var room = await _chatRepository.GetRoomByIdForUpdateAsync(roomId);
            if (room == null)
            {
                throw new NotFoundException(ErrorCodes.ChatNotAllowed, "Chat room not found");
            }

            if (room.User1Id != userId && room.User2Id != userId)
            {
                throw new ForbiddenException(ErrorCodes.ChatNotAllowed, "You are not a participant of this chat room");
            }

            // Only the RECIPIENT (not the requester) can accept/reject a pending request.
            if (room.RequestedByUserId == userId)
            {
                throw new ForbiddenException(ErrorCodes.ChatNotAllowed, "You cannot respond to your own chat request");
            }

            if (room.Status != ChatRoomStatuses.Pending)
            {
                throw new ConflictException(ErrorCodes.ChatRoomNotPending, "This chat request is no longer pending");
            }

            var now = DateTime.UtcNow;
            if (accept)
            {
                room.Status = ChatRoomStatuses.Active;
                room.AcceptedAt = now;
            }
            else
            {
                room.Status = ChatRoomStatuses.Rejected;
                room.RejectedAt = now;
            }

            await _chatRepository.SaveChangesAsync();

            if (accept && room.RequestedByUserId.HasValue)
            {
                await _notificationRepository.TryAddAndSaveAsync(new[]
                {
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = room.RequestedByUserId.Value,
                        Title = "Chat request accepted",
                        Content = "Your chat request was accepted",
                        Type = NotificationTypeConstants.Message,
                        CreatedAt = DateTime.UtcNow
                    }
                });
            }

            return room;
        }

        public async Task<Result<PagedResult<ChatMessageResponse>>> GetMessagesAsync(
            Guid userId,
            Guid roomId,
            ChatMessageFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var room = await _chatRepository.GetRoomByIdAsync(roomId);
            if (room == null)
            {
                throw new NotFoundException(
                    ErrorCodes.ChatNotAllowed,
                    "Chat room not found");
            }

            if (room.User1Id != userId && room.User2Id != userId)
            {
                throw new ForbiddenException(
                    ErrorCodes.ChatNotAllowed,
                    "You are not a participant of this chat room");
            }

            var (items, totalCount) = await _chatRepository.GetMessagesByRoomAsync(roomId, filter);

            var response = new PagedResult<ChatMessageResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<ChatMessageResponse>>.Success(response);
        }

        public async Task<Result<ChatMessageResponse>> SendMessageAsync(
            Guid userId,
            Guid roomId,
            SendMessageRequest request)
        {
            var validationResult = await _sendValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var room = await _chatRepository.GetRoomByIdForUpdateAsync(roomId);
            if (room == null)
            {
                throw new NotFoundException(
                    ErrorCodes.ChatNotAllowed,
                    "Chat room not found");
            }

            if (room.User1Id != userId && room.User2Id != userId)
            {
                throw new ForbiddenException(
                    ErrorCodes.ChatNotAllowed,
                    "You are not a participant of this chat room");
            }

            var receiverId = room.User1Id == userId ? room.User2Id : room.User1Id;

            if (room.Status == ChatRoomStatuses.Rejected)
            {
                throw new ConflictException(ErrorCodes.ChatRoomRejected, "This chat request was rejected");
            }

            // While pending, only the requester may keep sending — the recipient must accept first.
            if (room.Status == ChatRoomStatuses.Pending && room.RequestedByUserId != userId)
            {
                throw new ConflictException(ErrorCodes.ChatRoomNotPending, "Accept this chat request before replying");
            }

            if (await _userBlockRepository.IsBlockedEitherDirectionAsync(userId, receiverId))
            {
                throw new ForbiddenException(ErrorCodes.ChatUserBlocked, "You cannot message this user");
            }

            var now = DateTime.UtcNow;
            var message = new Message
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                SenderId = userId,
                Content = request.Content?.Trim() ?? string.Empty,
                IsRead = false,
                SentAt = now
            };

            await _chatRepository.AddMessageWithoutSaveAsync(message);

            var attachments = (request.Attachments ?? new List<SendMessageAttachmentRequest>())
                .Select(a => new MessageAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    CreatedAt = now
                })
                .ToList();

            if (attachments.Count > 0)
            {
                await _chatRepository.AddAttachmentsWithoutSaveAsync(attachments);
            }

            message.MessageAttachments = attachments;

            room.LastMessageAt = now;

            await _chatRepository.SaveChangesAsync();

            // Notify the OTHER participant — never the sender — only after the message is committed.
            var preview = string.IsNullOrEmpty(message.Content)
                ? "Sent an attachment"
                : (message.Content.Length > 80 ? message.Content[..80] + "…" : message.Content);

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = receiverId,
                Title = "New message",
                Content = preview,
                Type = NotificationTypeConstants.Message,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<ChatMessageResponse>.Success(message.ToResponse());
        }
    }
}
