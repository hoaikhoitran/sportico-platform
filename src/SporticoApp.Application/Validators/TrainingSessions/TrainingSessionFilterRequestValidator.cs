using FluentValidation;
using SporticoApp.Application.DTOs.TrainingSessions;

namespace SporticoApp.Application.Validators.TrainingSessions
{
    public class TrainingSessionFilterRequestValidator
        : AbstractValidator<TrainingSessionFilterRequest>
    {
        public TrainingSessionFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Status)
                .MaximumLength(20)
                .WithMessage("Status is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));

            RuleFor(x => x)
                .Must(x => !x.StartFrom.HasValue || !x.StartTo.HasValue || x.StartFrom <= x.StartTo)
                .WithMessage("StartFrom must be before StartTo");
        }
    }
}
