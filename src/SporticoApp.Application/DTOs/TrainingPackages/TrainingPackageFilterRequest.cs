namespace SporticoApp.Application.DTOs.TrainingPackages
{
    public class TrainingPackageFilterRequest
    {
        public string? Keyword { get; set; }

        public int? SportId { get; set; }

        public Guid? CoachId { get; set; }

        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
