using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;

namespace SporticoApp.Application.Validators.TrainingPackages
{
    public class UpdateTrainingPackageRequestValidator
        : AbstractValidator<UpdateTrainingPackageRequest>
    {
        public UpdateTrainingPackageRequestValidator()
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

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("StartDate is required");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("EndDate is required")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("EndDate must be on or after StartDate");

            RuleFor(x => x.Sessions)
                .NotEmpty()
                .WithMessage("Sessions are required");

            RuleForEach(x => x.Sessions)
                .SetValidator(new CreateTrainingPackageSessionRequestValidator());

            RuleFor(x => x).Custom((request, context) =>
                TrainingPackageScheduleValidator.Validate(
                    context, request.Sessions, request.SessionCount, request.StartDate, request.EndDate));

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
