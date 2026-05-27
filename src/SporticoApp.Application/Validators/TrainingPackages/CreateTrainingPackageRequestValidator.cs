using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;

namespace SporticoApp.Application.Validators.TrainingPackages
{
    public class CreateTrainingPackageRequestValidator
        : AbstractValidator<CreateTrainingPackageRequest>
    {
        public CreateTrainingPackageRequestValidator()
        {
            RuleFor(x => x.SportId)
                .GreaterThan(0)
                .WithMessage("SportId must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title is too long");

            RuleFor(x => x.Description)
                .MaximumLength(3000)
                .WithMessage("Description is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0")
                .Must(price => price % 1 == 0)
                .WithMessage("Price must be a whole number");

            RuleFor(x => x.SessionCount)
                .GreaterThan(0)
                .WithMessage("SessionCount must be greater than 0");

            RuleFor(x => x.DurationDays)
                .GreaterThan(0)
                .WithMessage("DurationDays must be greater than 0");

            RuleFor(x => x.Location)
                .MaximumLength(255)
                .WithMessage("Location is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Location));

            RuleFor(x => x.Level)
                .MaximumLength(50)
                .WithMessage("Level is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Level));

            RuleFor(x => x.GoalType)
                .MaximumLength(50)
                .WithMessage("GoalType is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.GoalType));
        }
    }
}
