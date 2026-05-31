namespace SporticoApp.Application.DTOs.Reviews
{
    public class CreateReviewRequest
    {
        public Guid CoachId { get; set; }

        /// <summary>Optional: a specific successful booking to attribute the review to.</summary>
        public Guid? BookingId { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}
