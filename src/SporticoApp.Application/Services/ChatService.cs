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
        private readonly ICoachRepository _coachRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CreateChatRoomRequest> _createRoomValidator;
        private readonly IValidator<ChatMessageFilterRequest> _filterValidator;
        private readonly IValidator<SendMessageRequest> _sendValidator;

        public ChatService(
            IChatRepository chatRepository,
            ICoachRepository coachRepository,
            INotificationRepository notificationRepository,
            IValidator<CreateChatRoomRequest> createRoomValidator,
            IValidator<ChatMessageFilterRequest> filterValidator,
            IValidator<SendMessageRequest> sendValidator)
        {
            _chatRepository = chatRepository;
            _coachRepository = coachRepository;
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
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            if (request.CoachId == userId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "You cannot open a chat room with yourself");
            }

            var coachExists = await _coachRepository.ExistsByUserIdAsync(request.CoachId);
            if (!coachExists)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachProfileNotFound,
                    "Coach not found");
            }

            var existing = await _chatRepository.GetRoomByUsersAsync(userId, request.CoachId);
            if (existing != null)
            {
                return Result<ChatRoomResponse>.Success(existing.ToResponse());
            }

            var user1Id = userId.CompareTo(request.CoachId) <= 0 ? userId : request.CoachId;
            var user2Id = userId.CompareTo(request.CoachId) <= 0 ? request.CoachId : userId;

            var room = new ChatRoom
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                CreatedAt = DateTime.UtcNow
            };

            var saved = await _chatRepository.AddRoomAsync(room);
            return Result<ChatRoomResponse>.Success(saved.ToResponse());
        }

        public async Task<Result<List<ChatRoomResponse>>> GetRoomsAsync(Guid userId)
        {
            var rooms = await _chatRepository.GetRoomsForUserAsync(userId);
            var response = rooms.Select(x => x.ToResponse()).ToList();
            return Result<List<ChatRoomResponse>>.Success(response);
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

            var message = new Message
            {
                Id = Guid.NewGuid(),
                RoomId = roomId,
                SenderId = userId,
                Content = request.Content.Trim(),
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            await _chatRepository.AddMessageAsync(message);

            // Notify the OTHER participant — never the sender.
            var receiverId = room.User1Id == userId ? room.User2Id : room.User1Id;
            var preview = message.Content.Length > 80
                ? message.Content[..80] + "…"
                : message.Content;

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
