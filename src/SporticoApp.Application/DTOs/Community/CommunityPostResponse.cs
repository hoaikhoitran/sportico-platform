namespace SporticoApp.Application.DTOs.Community
{
    public class CommunityPostMediaResponse
    {
        public Guid Id { get; set; }

        public string MediaType { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public int OrderIndex { get; set; }
    }

    public class CommunityPostAuthorResponse
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
    }

    public class CommunityPostResponse
    {
        public Guid Id { get; set; }

        public CommunityPostAuthorResponse Author { get; set; } = new();

        public int? SportId { get; set; }

        public string? SportName { get; set; }

        public string PostType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? LocationName { get; set; }

        public string? Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public int? MaxParticipants { get; set; }

        public int AcceptedParticipants { get; set; }

        public int? SlotsRemaining { get; set; }

        public string? Level { get; set; }

        public decimal? FeePerPerson { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool AllowComments { get; set; }

        public int CommentCount { get; set; }

        public int ReactionCount { get; set; }

        public int ApplicationCount { get; set; }

        public int ViewCount { get; set; }

        public List<CommunityPostMediaResponse> Media { get; set; } = new();

        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // ── Viewer-relative fields (only meaningful when a caller is known) ──
        public bool CurrentUserReacted { get; set; }

        public string? CurrentUserApplicationStatus { get; set; }

        public bool CanApply { get; set; }

        public bool CanEdit { get; set; }

        public bool CanModerate { get; set; }
    }
}
