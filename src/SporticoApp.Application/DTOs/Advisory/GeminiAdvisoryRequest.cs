namespace SporticoApp.Application.DTOs.Advisory
{
    /// <summary>
    /// Input passed from <c>AdvisoryService</c> to the Gemini provider: the current
    /// user message plus the prior conversation turns for context. The candidate
    /// training packages and coaches are gathered by the provider itself.
    /// </summary>
    public class GeminiAdvisoryRequest
    {
        public string UserMessage { get; set; } = string.Empty;

        public IReadOnlyList<AdvisoryMessageContext> History { get; set; } =
            new List<AdvisoryMessageContext>();
    }
}
