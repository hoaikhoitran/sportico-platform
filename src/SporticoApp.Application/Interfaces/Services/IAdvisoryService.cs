using SporticoApp.Application.DTOs.Advisory;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAdvisoryService
    {
        /// <summary>
        /// Sends a message to the advisory chatbot. Works identically for learner and admin
        /// callers — history is always scoped by <paramref name="userId"/>, never by role.
        /// </summary>
        /// <param name="userId">The authenticated caller's id.</param>
        /// <param name="initiatorRole">The caller's role, stored only when a new conversation is created.</param>
        Task<Result<AdvisoryReplyDto>> SendMessageAsync(
            Guid userId,
            string initiatorRole,
            SendAdvisoryMessageRequest request);
    }
}
