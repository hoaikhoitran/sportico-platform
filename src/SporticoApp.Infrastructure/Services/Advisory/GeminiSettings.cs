namespace SporticoApp.Infrastructure.Services.Advisory
{
    public class GeminiSettings
    {
        /// <summary>Google Generative Language API key. Never hardcode — supplied via configuration.</summary>
        public string ApiKey { get; set; } = string.Empty;

        public string Model { get; set; } = "gemini-2.0-flash";

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    }
}
