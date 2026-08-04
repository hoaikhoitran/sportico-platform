namespace SporticoApp.Application.DTOs.Community
{
    public class CreateApplicationRequest
    {
        public string? Message { get; set; }
    }

    public class CommunityApplicationFilterRequest
    {
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public class CommunityApplicationResponse
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public CommunityPostAuthorResponse Applicant { get; set; } = new();

        public string? Message { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime? CancelledAt { get; set; }
    }
}
