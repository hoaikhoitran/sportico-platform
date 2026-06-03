namespace SporticoApp.Application.DTOs.Users
{
    public class PublicUserResponse
    {
        public Guid Id { get; set; }

        public string? FullName { get; set; }

        public string? AvatarUrl { get; set; }

        /// <summary>Role names only — no ids or permissions.</summary>
        public List<string> Roles { get; set; } = new();

        public CoachProfileSummaryResponse? CoachProfile { get; set; }

        public LearnerProfileSummaryResponse? LearnerProfile { get; set; }
    }
}
