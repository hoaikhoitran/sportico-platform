using FluentValidation;
using SporticoApp.Application.DTOs.VisitorAnalytics;

namespace SporticoApp.Application.Validators.VisitorAnalytics
{
    public class VisitorAnalyticsFilterRequestValidator : AbstractValidator<VisitorAnalyticsFilterRequest>
    {
        public VisitorAnalyticsFilterRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be on or before ToDate");
        }
    }
}
