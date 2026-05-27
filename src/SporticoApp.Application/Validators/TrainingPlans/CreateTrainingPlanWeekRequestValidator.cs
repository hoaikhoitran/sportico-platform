using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPlans;

namespace SporticoApp.Application.Validators.TrainingPlans
{
    public class CreateTrainingPlanWeekRequestValidator
        : AbstractValidator<CreateTrainingPlanWeekRequest>
    {
        public CreateTrainingPlanWeekRequestValidator()
        {
            RuleFor(x => x.WeekNumber)
                .GreaterThan(0)
                .WithMessage("WeekNumber must be greater than 0");

            RuleFor(x => x.Focus)
                .MaximumLength(200)
                .WithMessage("Focus is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Focus));

            RuleFor(x => x.Notes)
                .MaximumLength(2000)
                .WithMessage("Notes is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
