using FluentValidation;
using SporticoApp.Application.DTOs.AdminPayments;

namespace SporticoApp.Application.Validators.AdminPayments
{
    public class RevenueChartFilterRequestValidator : AbstractValidator<RevenueChartFilterRequest>
    {
        private static readonly string[] AllowedGranularities = { "day", "week", "month", "year" };

        /// <summary>
        /// The chart is bucketed in-memory (see AdminPaymentRepository), so an unbounded range would
        /// pull an unbounded number of rows into memory. Cap it at 2 years.
        /// </summary>
        private const int MaxRangeDays = 730;

        public RevenueChartFilterRequestValidator()
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
                .WithMessage("Granularity must be one of: day, week, month, year")
                .When(x => !string.IsNullOrWhiteSpace(x.Granularity));
        }
    }
}
