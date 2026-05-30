using FluentValidation;
using SporticoApp.Application.DTOs.Availability;

namespace SporticoApp.Application.Validators.Availability
{
    public class CoachAvailabilitySlotFilterRequestValidator
        : AbstractValidator<CoachAvailabilitySlotFilterRequest>
    {
        public CoachAvailabilitySlotFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("PageNumber must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100");

            RuleFor(x => x.Status)
                .MaximumLength(20)
                .WithMessage("Status is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));

            RuleFor(x => x.StartTo)
                .GreaterThanOrEqualTo(x => x.StartFrom)
                .WithMessage("StartTo must be after or equal to StartFrom")
                .When(x => x.StartFrom.HasValue && x.StartTo.HasValue);
        }
    }
}
