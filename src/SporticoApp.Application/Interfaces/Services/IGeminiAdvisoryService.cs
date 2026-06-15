using SporticoApp.Application.DTOs.Advisory;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>
    /// Abstraction over the Gemini-backed advisory model. The implementation gathers the
    /// active training-package and coach catalog, calls the Gemini generateContent endpoint,
    /// and returns a safely parsed reply with validated coach recommendations.
    /// </summary>
    public interface IGeminiAdvisoryService
    {
        Task<GeminiAdvisoryResult> GenerateReplyAsync(
            GeminiAdvisoryRequest request,
            CancellationToken cancellationToken = default);
    }
}
