using SporticoApp.Application.DTOs.Advisory;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Services;
using SporticoApp.Application.Validators.Advisory;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Enums;
using SporticoApp.Shared.Exceptions;
using Xunit;
using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

namespace SporticoApp.Application.Tests.Advisory;

/// <summary>
/// The advisory chatbot is shared by learner and admin callers. These tests assert the handler
/// behaves identically for both — history and ownership are scoped by the caller's id, never by
/// role — and that every expected business case surfaces as a typed AppException or a successful
/// Result, never an unhandled exception.
/// </summary>
public class AdvisoryServiceTests
{
    private static readonly Guid LearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CoachId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static AdvisoryService CreateService(
        FakeAdvisoryConversationRepository repository,
        FakeGeminiAdvisoryService gemini)
        => new(repository, gemini, new SendAdvisoryMessageRequestValidator());

    [Theory]
    [InlineData("learner")]
    [InlineData("admin")]
    public async Task SendMessageAsync_NewConversation_PersistsTurnsAndReturnsReply(string role)
    {
        var userId = role == RoleConstants.Admin ? AdminId : LearnerId;
        var repository = new FakeAdvisoryConversationRepository();
        var gemini = new FakeGeminiAdvisoryService
        {
            Result = new GeminiAdvisoryResult
            {
                Reply = "Try interval training twice a week.",
                RecommendedCoachIds = new List<Guid> { CoachId }
            }
        };
        var service = CreateService(repository, gemini);

        var result = await service.SendMessageAsync(
            userId,
            role,
            new SendAdvisoryMessageRequest { Message = "How do I improve my stamina?" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Try interval training twice a week.", result.Data!.Reply);
        Assert.Equal(new[] { CoachId }, result.Data.RecommendedCoachIds);
        Assert.NotEqual(Guid.Empty, result.Data.ConversationId);

        // A new conversation was created for this user, tagged with the initiating role.
        var conversation = Assert.Single(repository.Conversations);
        Assert.Equal(userId, conversation.UserId);
        Assert.Equal(role, conversation.InitiatorRole);

        // Both the user message and the assistant reply were persisted in one save.
        Assert.Equal(1, repository.SaveCount);
        Assert.Collection(
            repository.Messages.OrderBy(m => m.Sender),
            m =>
            {
                Assert.Equal(AdvisorySenderConstants.Assistant, m.Sender);
                Assert.Equal("Try interval training twice a week.", m.Content);
            },
            m =>
            {
                Assert.Equal(AdvisorySenderConstants.User, m.Sender);
                Assert.Equal("How do I improve my stamina?", m.Content);
            });
    }

    [Theory]
    [InlineData("learner")]
    [InlineData("admin")]
    public async Task SendMessageAsync_ExistingOwnedConversation_PassesHistoryAndDoesNotCreateNew(string role)
    {
        var userId = role == RoleConstants.Admin ? AdminId : LearnerId;
        var conversationId = Guid.NewGuid();

        var repository = new FakeAdvisoryConversationRepository();
        repository.Conversations.Add(new AdvisoryConversation
        {
            Id = conversationId,
            UserId = userId,
            InitiatorRole = role
        });
        repository.Messages.Add(new AdvisoryMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Sender = AdvisorySenderConstants.User,
            Content = "earlier question",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        });

        var gemini = new FakeGeminiAdvisoryService
        {
            Result = new GeminiAdvisoryResult { Reply = "Here is more advice." }
        };
        var service = CreateService(repository, gemini);

        var result = await service.SendMessageAsync(
            userId,
            role,
            new SendAdvisoryMessageRequest { ConversationId = conversationId, Message = "follow up" });

        Assert.True(result.IsSuccess);
        Assert.Equal(conversationId, result.Data!.ConversationId);

        // No new conversation created — the existing one is reused.
        Assert.Single(repository.Conversations);

        // Prior turns were forwarded to the model as context.
        Assert.NotNull(gemini.LastRequest);
        Assert.Single(gemini.LastRequest!.History);
        Assert.Equal("earlier question", gemini.LastRequest.History[0].Content);
        Assert.Equal("follow up", gemini.LastRequest.UserMessage);
    }

    [Fact]
    public async Task SendMessageAsync_ConversationOwnedByAnotherUser_ThrowsForbidden()
    {
        var conversationId = Guid.NewGuid();
        var repository = new FakeAdvisoryConversationRepository();
        repository.Conversations.Add(new AdvisoryConversation
        {
            Id = conversationId,
            UserId = OtherUserId,
            InitiatorRole = RoleConstants.Learner
        });
        var service = CreateService(repository, new FakeGeminiAdvisoryService());

        // An admin caller is still forbidden from another user's conversation: scoping is by id, not role.
        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SendMessageAsync(
                AdminId,
                RoleConstants.Admin,
                new SendAdvisoryMessageRequest { ConversationId = conversationId, Message = "hi" }));

        Assert.Equal(ErrorType.Forbidden, ex.Type);
    }

    [Fact]
    public async Task SendMessageAsync_MissingConversation_ThrowsNotFound()
    {
        var service = CreateService(new FakeAdvisoryConversationRepository(), new FakeGeminiAdvisoryService());

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.SendMessageAsync(
                LearnerId,
                RoleConstants.Learner,
                new SendAdvisoryMessageRequest { ConversationId = Guid.NewGuid(), Message = "hi" }));

        Assert.Equal(ErrorType.NotFound, ex.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendMessageAsync_EmptyMessage_ThrowsValidation(string message)
    {
        var service = CreateService(new FakeAdvisoryConversationRepository(), new FakeGeminiAdvisoryService());

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.SendMessageAsync(
                LearnerId,
                RoleConstants.Learner,
                new SendAdvisoryMessageRequest { Message = message }));

        Assert.Equal(ErrorType.Validation, ex.Type);
    }

    private sealed class FakeAdvisoryConversationRepository : IAdvisoryConversationRepository
    {
        public readonly List<AdvisoryConversation> Conversations = new();
        public readonly List<AdvisoryMessage> Messages = new();
        public int SaveCount;

        public Task<AdvisoryConversation?> GetByIdForUpdateAsync(Guid conversationId)
            => Task.FromResult(Conversations.FirstOrDefault(c => c.Id == conversationId));

        public Task<List<AdvisoryMessage>> GetRecentMessagesAsync(Guid conversationId, int limit)
            => Task.FromResult(Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .TakeLast(limit)
                .ToList());

        public Task AddConversationWithoutSaveAsync(AdvisoryConversation conversation)
        {
            Conversations.Add(conversation);
            return Task.CompletedTask;
        }

        public Task AddMessageWithoutSaveAsync(AdvisoryMessage message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGeminiAdvisoryService : IGeminiAdvisoryService
    {
        public GeminiAdvisoryResult Result { get; set; } = new() { Reply = "ok" };
        public GeminiAdvisoryRequest? LastRequest { get; private set; }

        public Task<GeminiAdvisoryResult> GenerateReplyAsync(
            GeminiAdvisoryRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }
}
