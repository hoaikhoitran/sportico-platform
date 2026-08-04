namespace SporticoApp.Application.DTOs.Community
{
    public class AdminCommunityPostFilterRequest
    {
        public string? Status { get; set; }

        public string? PostType { get; set; }

        public int? SportId { get; set; }

        public Guid? AuthorId { get; set; }

        public string? Keyword { get; set; }

        public bool ReportedOnly { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string SortBy { get; set; } = "latest";

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public class HideContentRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminCommunityPostResponse
    {
        public Guid Id { get; set; }

        public CommunityPostAuthorResponse Author { get; set; } = new();

        public string PostType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? ModerationReason { get; set; }

        public int ReportCount { get; set; }

        public int CommentCount { get; set; }

        public int ReactionCount { get; set; }

        public int ApplicationCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public DateTime? HiddenAt { get; set; }

        public DateTime? DeletedAt { get; set; }
    }

    public class CreateReportRequest
    {
        /// <summary>community_post | community_comment | chat_message.</summary>
        public string TargetType { get; set; } = string.Empty;

        public Guid TargetId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class AdminReportFilterRequest
    {
        public string? TargetType { get; set; }

        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public class ResolveReportRequest
    {
        /// <summary>resolved | rejected.</summary>
        public string Status { get; set; } = string.Empty;

        public string? ResolutionNote { get; set; }

        /// <summary>none | post_hidden | post_deleted | comment_hidden | comment_deleted.</summary>
        public string ActionTaken { get; set; } = "none";
    }

    public class ReportResponse
    {
        public Guid Id { get; set; }

        public Guid ReporterId { get; set; }

        public string TargetType { get; set; } = string.Empty;

        public Guid? TargetId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;

        public Guid? HandledByUserId { get; set; }

        public DateTime? HandledAt { get; set; }

        public string? ResolutionNote { get; set; }

        public string? ActionTaken { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
