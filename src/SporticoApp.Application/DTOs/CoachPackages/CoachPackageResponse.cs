namespace SporticoApp.Application.DTOs.CoachPackages
{
    public class CoachPackageResponse
    {
        public Guid Id { get; set; }

        public Guid CoachId { get; set; }

        public int PackageId { get; set; }

        public string PackageName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int RemainingPosts { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
