using System.Text.Json;

namespace SporticoApp.Application.DTOs.Payments
{
    public class PayOsWebhookRequest
    {
        public JsonElement Data { get; set; }

        public string? Signature { get; set; }
    }
}
