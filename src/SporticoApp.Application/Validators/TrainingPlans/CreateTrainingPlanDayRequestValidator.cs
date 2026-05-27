using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPlans;

namespace SporticoApp.Application.Validators.TrainingPlans
{
    public class CreateTrainingPlanDayRequestValidator
        : AbstractValidator<CreateTrainingPlanDayRequest>
    {
        public CreateTrainingPlanDayRequestValidator()
        {
            RuleFor(x => x.DayNumber)
                .GreaterThan(0)
                .WithMessage("DayNumber must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title is too long");

            RuleFor(x => x.Notes)
                .MaximumLength(2000)
                .WithMessage("Notes is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
