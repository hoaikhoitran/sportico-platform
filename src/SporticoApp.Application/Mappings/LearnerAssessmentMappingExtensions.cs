using SporticoApp.Application.DTOs.Assessments;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class LearnerAssessmentMappingExtensions
    {
        public static LearnerAssessment ToEntity(
            this CreateLearnerAssessmentRequest request,
            Guid bookingId,
            Guid learnerId,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new LearnerAssessment
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                LearnerId = learnerId,
                CoachId = coachId,
                GoalType = request.GoalType.Trim(),
                GoalDescription = request.GoalDescription?.Trim(),
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                BodyFatPercent = request.BodyFatPercent,
                CurrentLevel = request.CurrentLevel?.Trim(),
                HealthNotes = request.HealthNotes?.Trim(),
                InjuryNotes = request.InjuryNotes?.Trim(),
                TrainingHistory = request.TrainingHistory?.Trim(),
                AvailableDaysPerWeek = request.AvailableDaysPerWeek?.Trim(),
                PreferredSessionDurationMinutes = request.PreferredSessionDurationMinutes,
                EquipmentAvailable = request.EquipmentAvailable?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this LearnerAssessment assessment,
            UpdateLearnerAssessmentRequest request)
        {
            assessment.GoalType = request.GoalType.Trim();
            assessment.GoalDescription = request.GoalDescription?.Trim();
            assessment.HeightCm = request.HeightCm;
            assessment.WeightKg = request.WeightKg;
            assessment.BodyFatPercent = request.BodyFatPercent;
            assessment.CurrentLevel = request.CurrentLevel?.Trim();
            assessment.HealthNotes = request.HealthNotes?.Trim();
            assessment.InjuryNotes = request.InjuryNotes?.Trim();
            assessment.TrainingHistory = request.TrainingHistory?.Trim();
            assessment.AvailableDaysPerWeek = request.AvailableDaysPerWeek?.Trim();
            assessment.PreferredSessionDurationMinutes = request.PreferredSessionDurationMinutes;
            assessment.EquipmentAvailable = request.EquipmentAvailable?.Trim();
            assessment.UpdatedAt = DateTime.UtcNow;
        }

        public static LearnerAssessmentResponse ToResponse(this LearnerAssessment assessment)
        {
            return new LearnerAssessmentResponse
            {
                Id = assessment.Id,
                BookingId = assessment.BookingId,
                LearnerId = assessment.LearnerId,
                CoachId = assessment.CoachId,
                GoalType = assessment.GoalType,
                GoalDescription = assessment.GoalDescription,
                HeightCm = assessment.HeightCm,
                WeightKg = assessment.WeightKg,
                BodyFatPercent = assessment.BodyFatPercent,
                CurrentLevel = assessment.CurrentLevel,
                HealthNotes = assessment.HealthNotes,
                InjuryNotes = assessment.InjuryNotes,
                TrainingHistory = assessment.TrainingHistory,
                AvailableDaysPerWeek = assessment.AvailableDaysPerWeek,
                PreferredSessionDurationMinutes = assessment.PreferredSessionDurationMinutes,
                EquipmentAvailable = assessment.EquipmentAvailable,
                CreatedAt = assessment.CreatedAt,
                UpdatedAt = assessment.UpdatedAt
            };
        }
    }
}
