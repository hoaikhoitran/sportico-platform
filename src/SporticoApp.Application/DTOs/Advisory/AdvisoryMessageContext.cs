namespace SporticoApp.Application.DTOs.Advisory
{
    /// <summary>
    /// A prior conversation turn passed to the AI provider as context.
    /// </summary>
    public class AdvisoryMessageContext
    {
        public string Sender { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
