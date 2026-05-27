namespace SporticoApp.Application.DTOs.TrainingPackages
{
    public class CreateTrainingPackageRequest
    {
        public int SportId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int SessionCount { get; set; }

        public int DurationDays { get; set; }

        public string? Location { get; set; }

        public bool IsOnline { get; set; }

        public string? Level { get; set; }

        public string? GoalType { get; set; }
    }
}
