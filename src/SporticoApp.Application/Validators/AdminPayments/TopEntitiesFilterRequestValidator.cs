using FluentValidation;
using SporticoApp.Application.DTOs.AdminPayments;

namespace SporticoApp.Application.Validators.AdminPayments
{
    public class TopEntitiesFilterRequestValidator : AbstractValidator<TopEntitiesFilterRequest>
    {
        public TopEntitiesFilterRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be on or before ToDate");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 50)
                .WithMessage("Limit must be between 1 and 50");
        }
    }
}
