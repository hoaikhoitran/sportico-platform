using FluentValidation;
using SporticoApp.Application.DTOs.VisitorAnalytics;

namespace SporticoApp.Application.Validators.VisitorAnalytics
{
    public class VisitorsChartFilterRequestValidator : AbstractValidator<VisitorsChartFilterRequest>
    {
        private static readonly string[] AllowedGranularities = { "hour", "day", "week", "month", "year" };

        /// <summary>
        /// The chart is bucketed in-memory (see VisitorAnalyticsRepository), so an unbounded range
        /// would pull an unbounded number of rows into memory. Cap it at 2 years — tighter for
        /// hourly granularity is left as a future improvement (see final report).
        /// </summary>
        private const int MaxRangeDays = 730;

        public VisitorsChartFilterRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be on or before ToDate");

            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue ||
                           (x.ToDate.Value - x.FromDate.Value).TotalDays <= MaxRangeDays)
                .WithMessage($"Date range must not exceed {MaxRangeDays} days");

            RuleFor(x => x.Granularity)
                .Must(g => AllowedGranularities.Contains(g!.Trim().ToLowerInvariant()))
                .WithMessage("Granularity must be one of: hour, day, week, month, year")
                .When(x => !string.IsNullOrWhiteSpace(x.Granularity));
        }
    }
}
