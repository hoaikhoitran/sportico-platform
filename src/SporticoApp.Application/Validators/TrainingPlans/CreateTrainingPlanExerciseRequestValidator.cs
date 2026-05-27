using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPlans;

namespace SporticoApp.Application.Validators.TrainingPlans
{
    public class CreateTrainingPlanExerciseRequestValidator
        : AbstractValidator<CreateTrainingPlanExerciseRequest>
    {
        public CreateTrainingPlanExerciseRequestValidator()
        {
            RuleFor(x => x.ExerciseName)
                .NotEmpty()
                .WithMessage("ExerciseName is required")
                .MaximumLength(200)
                .WithMessage("ExerciseName is too long");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("OrderIndex must be 0 or greater");

            RuleFor(x => x.Sets)
                .GreaterThan(0)
                .WithMessage("Sets must be greater than 0")
                .When(x => x.Sets.HasValue);

            RuleFor(x => x.Reps)
                .MaximumLength(50)
                .WithMessage("Reps is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Reps));

            RuleFor(x => x.Intensity)
                .MaximumLength(50)
                .WithMessage("Intensity is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Intensity));

            RuleFor(x => x.RestSeconds)
                .GreaterThanOrEqualTo(0)
                .WithMessage("RestSeconds must be 0 or greater")
                .When(x => x.RestSeconds.HasValue);

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        }
    }
}
