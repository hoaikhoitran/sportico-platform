namespace SporticoApp.Application.DTOs.Advisory
{
    /// <summary>
    /// Parsed Gemini advisory output: the assistant reply and the coach ids it
    /// recommended (already validated against the active coach catalog).
    /// </summary>
    public class GeminiAdvisoryResult
    {
        public string Reply { get; set; } = string.Empty;

        public List<Guid> RecommendedCoachIds { get; set; } = new();
    }
}
