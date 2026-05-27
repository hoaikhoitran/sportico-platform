using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;

namespace SporticoApp.Application.Validators.TrainingPackages
{
    public class TrainingPackageFilterRequestValidator
        : AbstractValidator<TrainingPackageFilterRequest>
    {
        public TrainingPackageFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Keyword)
                .MaximumLength(200)
                .WithMessage("Keyword is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Keyword));

            RuleFor(x => x.Status)
                .MaximumLength(20)
                .WithMessage("Status is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));
        }
    }
}
