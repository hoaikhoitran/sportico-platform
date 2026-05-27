using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPlans;

namespace SporticoApp.Application.Validators.TrainingPlans
{
    public class UpdateTrainingPlanRequestValidator
        : AbstractValidator<UpdateTrainingPlanRequest>
    {
        public UpdateTrainingPlanRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title is too long");

            RuleFor(x => x.GoalType)
                .NotEmpty()
                .WithMessage("GoalType is required")
                .MaximumLength(50)
                .WithMessage("GoalType is too long");

            RuleFor(x => x.Overview)
                .MaximumLength(3000)
                .WithMessage("Overview is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Overview));

            RuleFor(x => x.TotalWeeks)
                .GreaterThan(0)
                .WithMessage("TotalWeeks must be greater than 0");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("EndDate must be after StartDate");

            RuleFor(x => x.Status)
                .MaximumLength(20)
                .WithMessage("Status is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));
        }
    }
}
