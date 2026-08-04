namespace SporticoApp.Application.DTOs.Community
{
    public class CommunityPostMediaRequest
    {
        public string MediaType { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public string? MimeType { get; set; }

        public long? FileSize { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? DurationSeconds { get; set; }
    }

    public class CreateCommunityPostRequest
    {
        public int? SportId { get; set; }

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

        public string? Level { get; set; }

        public decimal? FeePerPerson { get; set; }

        public bool AllowComments { get; set; } = true;

        /// <summary>Publish immediately (true) or save as draft (false).</summary>
        public bool Publish { get; set; } = true;

        public List<CommunityPostMediaRequest>? Media { get; set; }
    }

    public class UpdateCommunityPostRequest
    {
        public string? Title { get; set; }

        public string? Content { get; set; }

        public string? LocationName { get; set; }

        public string? Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public int? MaxParticipants { get; set; }

        public string? Level { get; set; }

        public decimal? FeePerPerson { get; set; }

        public bool? AllowComments { get; set; }

        public List<CommunityPostMediaRequest>? Media { get; set; }
    }

    public class CommunityPostFilterRequest
    {
        public string? PostType { get; set; }

        public int? SportId { get; set; }

        public string? Keyword { get; set; }

        public string? City { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? Level { get; set; }

        public bool? HasAvailableSlots { get; set; }

        public Guid? AuthorId { get; set; }

        public bool FollowingOnly { get; set; }

        /// <summary>latest | upcoming | most_discussed.</summary>
        public string SortBy { get; set; } = "latest";

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
