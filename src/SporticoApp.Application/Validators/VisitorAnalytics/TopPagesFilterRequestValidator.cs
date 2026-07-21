using FluentValidation;
using SporticoApp.Application.DTOs.VisitorAnalytics;

namespace SporticoApp.Application.Validators.VisitorAnalytics
{
    public class TopPagesFilterRequestValidator : AbstractValidator<TopPagesFilterRequest>
    {
        public TopPagesFilterRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be on or before ToDate");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 100)
                .WithMessage("Limit must be between 1 and 100");
        }
    }
}
