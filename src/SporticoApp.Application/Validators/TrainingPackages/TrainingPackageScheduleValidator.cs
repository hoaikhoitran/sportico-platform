using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;

namespace SporticoApp.Application.Validators.TrainingPackages
{
    /// <summary>
    /// Shared cross-session schedule rules used by both the create and update package validators.
    /// Enforces: schedule size == SessionCount, session numbers are exactly 1..SessionCount (unique),
    /// every session sits within [StartDate, EndDate], and no two sessions in the package overlap.
    /// </summary>
    public static class TrainingPackageScheduleValidator
    {
        public static void Validate<T>(
            ValidationContext<T> context,
            IReadOnlyList<CreateTrainingPackageSessionRequest>? sessions,
            int sessionCount,
            DateTime startDate,
            DateTime endDate)
        {
            sessions ??= Array.Empty<CreateTrainingPackageSessionRequest>();

            if (sessions.Count != sessionCount)
            {
                context.AddFailure(
                    $"Sessions count ({sessions.Count}) must equal SessionCount ({sessionCount})");
                return;
            }

            if (sessions.Count == 0)
            {
                return;
            }

            // Session numbers must be exactly the contiguous set 1..SessionCount (unique).
            var expected = Enumerable.Range(1, sessionCount).ToHashSet();
            var actual = sessions.Select(s => s.SessionNumber).ToList();
            if (actual.Distinct().Count() != actual.Count ||
                !actual.ToHashSet().SetEquals(expected))
            {
                context.AddFailure(
                    $"SessionNumber values must be unique and cover 1..{sessionCount}");
            }

            // Every session must fall within the package date range (inclusive, by calendar day).
            var rangeStart = startDate.Date;
            var rangeEnd = endDate.Date;
            foreach (var session in sessions)
            {
                if (session.StartTime.Date < rangeStart || session.EndTime.Date > rangeEnd)
                {
                    context.AddFailure(
                        $"Session {session.SessionNumber} must fall within StartDate and EndDate");
                }
            }

            // No two sessions in the same package may overlap in time.
            var ordered = sessions
                .OrderBy(s => s.StartTime)
                .ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var curr = ordered[i];
                if (curr.StartTime < prev.EndTime && curr.EndTime > prev.StartTime)
                {
                    context.AddFailure(
                        $"Sessions {prev.SessionNumber} and {curr.SessionNumber} overlap in time");
                }
            }
        }
    }
}
