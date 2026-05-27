namespace SporticoApp.Application.DTOs.TrainingSessions
{
    public class TrainingSessionFilterRequest
    {
        public string? Status { get; set; }

        public DateTime? StartFrom { get; set; }

        public DateTime? StartTo { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
