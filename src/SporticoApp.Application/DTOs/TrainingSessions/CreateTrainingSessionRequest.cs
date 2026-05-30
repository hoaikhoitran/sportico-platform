namespace SporticoApp.Application.DTOs.TrainingSessions
{
    public class CreateTrainingSessionRequest
    {
        /// <summary>Set by the controller from the route parameter {bookingId}.</summary>
        public Guid BookingId { get; set; }

        public Guid AvailabilitySlotId { get; set; }

        public string? LearnerNote { get; set; }
    }
}
