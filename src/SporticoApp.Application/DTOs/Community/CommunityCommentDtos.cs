namespace SporticoApp.Application.DTOs.Community
{
    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CreateReplyRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CommunityCommentFilterRequest
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public class CommunityCommentResponse
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public CommunityPostAuthorResponse Author { get; set; } = new();

        public Guid? ParentCommentId { get; set; }

        /// <summary>"Bình luận đã bị xóa" placeholder when Status == deleted, otherwise the real content.</summary>
        public string Content { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int ReplyCount { get; set; }

        public List<CommunityCommentResponse> Replies { get; set; } = new();

        public bool CanEdit { get; set; }

        public bool CanModerate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
