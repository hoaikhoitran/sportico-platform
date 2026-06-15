using FluentValidation;
using SporticoApp.Application.DTOs.Advisory;
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

    public class AdvisoryService : IAdvisoryService
    {
        /// <summary>Number of prior turns sent to the model as conversation context.</summary>
        private const int HistoryLimit = 20;

        private readonly IAdvisoryConversationRepository _advisoryRepository;
        private readonly IGeminiAdvisoryService _geminiAdvisoryService;
        private readonly IValidator<SendAdvisoryMessageRequest> _sendValidator;

        public AdvisoryService(
            IAdvisoryConversationRepository advisoryRepository,
            IGeminiAdvisoryService geminiAdvisoryService,
            IValidator<SendAdvisoryMessageRequest> sendValidator)
        {
            _advisoryRepository = advisoryRepository;
            _geminiAdvisoryService = geminiAdvisoryService;
            _sendValidator = sendValidator;
        }

        public async Task<Result<AdvisoryReplyDto>> SendMessageAsync(
            Guid userId,
            string initiatorRole,
            SendAdvisoryMessageRequest request)
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

            var message = request.Message.Trim();

            var (conversation, history) = await ResolveConversationAsync(userId, initiatorRole, request);

            await _advisoryRepository.AddMessageWithoutSaveAsync(new AdvisoryMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Sender = AdvisorySenderConstants.User,
                Content = message,
                CreatedAt = DateTime.UtcNow
            });

            var geminiResult = await _geminiAdvisoryService.GenerateReplyAsync(new GeminiAdvisoryRequest
            {
                UserMessage = message,
                History = history
            });

            await _advisoryRepository.AddMessageWithoutSaveAsync(new AdvisoryMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Sender = AdvisorySenderConstants.Assistant,
                Content = geminiResult.Reply,
                CreatedAt = DateTime.UtcNow
            });

            conversation.UpdatedAt = DateTime.UtcNow;

            await _advisoryRepository.SaveChangesAsync();

            return Result<AdvisoryReplyDto>.Success(new AdvisoryReplyDto
            {
                ConversationId = conversation.Id,
                Reply = geminiResult.Reply,
                RecommendedCoachIds = geminiResult.RecommendedCoachIds
            });
        }

        /// <summary>
        /// Loads the requested conversation (verifying it belongs to the caller) or creates a new
        /// one. Returns the conversation together with its prior turns to feed to the model.
        /// </summary>
        private async Task<(AdvisoryConversation Conversation, IReadOnlyList<AdvisoryMessageContext> History)>
            ResolveConversationAsync(Guid userId, string initiatorRole, SendAdvisoryMessageRequest request)
        {
            if (request.ConversationId.HasValue)
            {
                var existing = await _advisoryRepository
                    .GetByIdForUpdateAsync(request.ConversationId.Value);

                if (existing == null)
                {
                    throw new NotFoundException(
                        ErrorCodes.AdvisoryConversationNotFound,
                        "Advisory conversation not found");
                }

                // Scope strictly by the caller's id so a learner and an admin are handled identically.
                if (existing.UserId != userId)
                {
                    throw new ForbiddenException(
                        ErrorCodes.AdvisoryConversationNotOwned,
                        "You are not the owner of this advisory conversation");
                }

                var priorMessages = await _advisoryRepository
                    .GetRecentMessagesAsync(existing.Id, HistoryLimit);

                var history = priorMessages
                    .Select(x => x.ToContext())
                    .ToList();

                return (existing, history);
            }

            var conversation = new AdvisoryConversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                InitiatorRole = initiatorRole,
                Title = BuildTitle(request.Message),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _advisoryRepository.AddConversationWithoutSaveAsync(conversation);

            return (conversation, new List<AdvisoryMessageContext>());
        }

        private static string BuildTitle(string message)
        {
            var trimmed = message.Trim();
            return trimmed.Length > 80 ? trimmed[..80] : trimmed;
        }
    }
}
