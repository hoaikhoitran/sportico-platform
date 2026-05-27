using SporticoApp.Application.DTOs.TrainingPlans;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Mappings
{
    public static class TrainingPlanMappingExtensions
    {
        public static TrainingPlan ToEntity(
            this CreateTrainingPlanRequest request,
            Guid bookingId,
            Guid learnerId,
            Guid coachId)
        {
            var now = DateTime.UtcNow;

            return new TrainingPlan
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                LearnerId = learnerId,
                CoachId = coachId,
                Title = request.Title.Trim(),
                GoalType = request.GoalType.Trim(),
                Overview = request.Overview?.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                TotalWeeks = request.TotalWeeks,
                Status = TrainingPlanStatuses.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        public static void ApplyUpdate(
            this TrainingPlan plan,
            UpdateTrainingPlanRequest request)
        {
            plan.Title = request.Title.Trim();
            plan.GoalType = request.GoalType.Trim();
            plan.Overview = request.Overview?.Trim();
            plan.StartDate = request.StartDate;
            plan.EndDate = request.EndDate;
            plan.TotalWeeks = request.TotalWeeks;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                plan.Status = request.Status.Trim().ToLowerInvariant();
            }
            plan.UpdatedAt = DateTime.UtcNow;
        }

        public static TrainingPlanWeek ToEntity(
            this CreateTrainingPlanWeekRequest request,
            Guid trainingPlanId)
        {
            return new TrainingPlanWeek
            {
                Id = Guid.NewGuid(),
                TrainingPlanId = trainingPlanId,
                WeekNumber = request.WeekNumber,
                Focus = request.Focus?.Trim(),
                Notes = request.Notes?.Trim()
            };
        }

        public static TrainingPlanDay ToEntity(
            this CreateTrainingPlanDayRequest request,
            Guid trainingPlanWeekId)
        {
            return new TrainingPlanDay
            {
                Id = Guid.NewGuid(),
                TrainingPlanWeekId = trainingPlanWeekId,
                DayNumber = request.DayNumber,
                Title = request.Title.Trim(),
                Notes = request.Notes?.Trim()
            };
        }

        public static TrainingPlanExercise ToEntity(
            this CreateTrainingPlanExerciseRequest request,
            Guid trainingPlanDayId)
        {
            return new TrainingPlanExercise
            {
                Id = Guid.NewGuid(),
                TrainingPlanDayId = trainingPlanDayId,
                ExerciseName = request.ExerciseName.Trim(),
                OrderIndex = request.OrderIndex,
                Sets = request.Sets,
                Reps = request.Reps?.Trim(),
                Intensity = request.Intensity?.Trim(),
                RestSeconds = request.RestSeconds,
                Notes = request.Notes?.Trim()
            };
        }

        public static void ApplyUpdate(
            this TrainingPlanExercise exercise,
            UpdateTrainingPlanExerciseRequest request)
        {
            exercise.ExerciseName = request.ExerciseName.Trim();
            exercise.OrderIndex = request.OrderIndex;
            exercise.Sets = request.Sets;
            exercise.Reps = request.Reps?.Trim();
            exercise.Intensity = request.Intensity?.Trim();
            exercise.RestSeconds = request.RestSeconds;
            exercise.Notes = request.Notes?.Trim();
        }

        public static TrainingPlanResponse ToResponse(this TrainingPlan plan)
        {
            return new TrainingPlanResponse
            {
                Id = plan.Id,
                BookingId = plan.BookingId,
                LearnerId = plan.LearnerId,
                CoachId = plan.CoachId,
                Title = plan.Title,
                GoalType = plan.GoalType,
                Overview = plan.Overview,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                TotalWeeks = plan.TotalWeeks,
                Status = plan.Status,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                Weeks = plan.Weeks
                    .OrderBy(x => x.WeekNumber)
                    .Select(x => x.ToResponse())
                    .ToList()
            };
        }

        public static TrainingPlanWeekResponse ToResponse(this TrainingPlanWeek week)
        {
            return new TrainingPlanWeekResponse
            {
                Id = week.Id,
                TrainingPlanId = week.TrainingPlanId,
                WeekNumber = week.WeekNumber,
                Focus = week.Focus,
                Notes = week.Notes,
                Days = week.Days
                    .OrderBy(x => x.DayNumber)
                    .Select(x => x.ToResponse())
                    .ToList()
            };
        }

        public static TrainingPlanDayResponse ToResponse(this TrainingPlanDay day)
        {
            return new TrainingPlanDayResponse
            {
                Id = day.Id,
                TrainingPlanWeekId = day.TrainingPlanWeekId,
                DayNumber = day.DayNumber,
                Title = day.Title,
                Notes = day.Notes,
                Exercises = day.Exercises
                    .OrderBy(x => x.OrderIndex)
                    .Select(x => x.ToResponse())
                    .ToList()
            };
        }

        public static TrainingPlanExerciseResponse ToResponse(this TrainingPlanExercise exercise)
        {
            return new TrainingPlanExerciseResponse
            {
                Id = exercise.Id,
                TrainingPlanDayId = exercise.TrainingPlanDayId,
                ExerciseName = exercise.ExerciseName,
                OrderIndex = exercise.OrderIndex,
                Sets = exercise.Sets,
                Reps = exercise.Reps,
                Intensity = exercise.Intensity,
                RestSeconds = exercise.RestSeconds,
                Notes = exercise.Notes
            };
        }
    }
}
