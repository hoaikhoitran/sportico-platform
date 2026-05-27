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
        private readonly IBookingRepository _bookingRepository;
        private readonly IValidator<ChatMessageFilterRequest> _filterValidator;
        private readonly IValidator<SendMessageRequest> _sendValidator;

        public ChatService(
            IChatRepository chatRepository,
            IBookingRepository bookingRepository,
            IValidator<ChatMessageFilterRequest> filterValidator,
            IValidator<SendMessageRequest> sendValidator)
        {
            _chatRepository = chatRepository;
            _bookingRepository = bookingRepository;
            _filterValidator = filterValidator;
            _sendValidator = sendValidator;
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
                    "Chat is not allowed");
            }

            await EnsureChatAllowedAsync(room.User1Id, room.User2Id);

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
                    "Chat is not allowed");
            }

            await EnsureChatAllowedAsync(room.User1Id, room.User2Id);

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

            return Result<ChatMessageResponse>.Success(message.ToResponse());
        }

        private async Task EnsureChatAllowedAsync(Guid user1Id, Guid user2Id)
        {
            // Room participants are stored ordered by GUID, not as (learner, coach),
            // so check both orderings against the booking's (LearnerId, CoachId).
            var booking =
                await _bookingRepository.GetActiveOrCompletedBetweenUsersAsync(user1Id, user2Id)
                ?? await _bookingRepository.GetActiveOrCompletedBetweenUsersAsync(user2Id, user1Id);
            if (booking == null)
            {
                throw new ForbiddenException(
                    ErrorCodes.ChatNotAllowed,
                    "Chat is not allowed without an active booking");
            }
        }
    }
}
