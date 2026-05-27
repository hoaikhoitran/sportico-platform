using SporticoApp.Application.DTOs.ProgressCheckIns;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class ProgressCheckInMappingExtensions
    {
        public static ProgressCheckIn ToEntity(
            this CreateProgressCheckInRequest request,
            Guid bookingId,
            Guid learnerId,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new ProgressCheckIn
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                LearnerId = learnerId,
                CoachId = coachId,
                CheckInDate = request.CheckInDate,
                WeightKg = request.WeightKg,
                BodyFatPercent = request.BodyFatPercent,
                WaistCm = request.WaistCm,
                EnergyLevel = request.EnergyLevel?.Trim(),
                SleepQuality = request.SleepQuality?.Trim(),
                LearnerNote = request.LearnerNote?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static ProgressCheckInResponse ToResponse(this ProgressCheckIn checkIn)
        {
            return new ProgressCheckInResponse
            {
                Id = checkIn.Id,
                BookingId = checkIn.BookingId,
                LearnerId = checkIn.LearnerId,
                CoachId = checkIn.CoachId,
                CheckInDate = checkIn.CheckInDate,
                WeightKg = checkIn.WeightKg,
                BodyFatPercent = checkIn.BodyFatPercent,
                WaistCm = checkIn.WaistCm,
                EnergyLevel = checkIn.EnergyLevel,
                SleepQuality = checkIn.SleepQuality,
                LearnerNote = checkIn.LearnerNote,
                CoachFeedback = checkIn.CoachFeedback,
                CreatedAt = checkIn.CreatedAt,
                UpdatedAt = checkIn.UpdatedAt
            };
        }
    }
}
