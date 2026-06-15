namespace SporticoApp.Application.DTOs.Advisory
{
    public class AdvisoryReplyDto
    {
        public Guid ConversationId { get; set; }

        public string Reply { get; set; } = string.Empty;

        public List<Guid> RecommendedCoachIds { get; set; } = new();
    }
}
