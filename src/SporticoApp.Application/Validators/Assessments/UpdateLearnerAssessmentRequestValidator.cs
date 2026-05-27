using FluentValidation;
using SporticoApp.Application.DTOs.Assessments;

namespace SporticoApp.Application.Validators.Assessments
{
    public class UpdateLearnerAssessmentRequestValidator
        : AbstractValidator<UpdateLearnerAssessmentRequest>
    {
        public UpdateLearnerAssessmentRequestValidator()
        {
            RuleFor(x => x.GoalType)
                .NotEmpty()
                .WithMessage("GoalType is required")
                .MaximumLength(50)
                .WithMessage("GoalType is too long");

            RuleFor(x => x.GoalDescription)
                .MaximumLength(2000)
                .WithMessage("GoalDescription is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.GoalDescription));

            RuleFor(x => x.CurrentLevel)
                .MaximumLength(50)
                .WithMessage("CurrentLevel is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.CurrentLevel));

            RuleFor(x => x.HealthNotes)
                .MaximumLength(3000)
                .WithMessage("HealthNotes is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.HealthNotes));

            RuleFor(x => x.InjuryNotes)
                .MaximumLength(3000)
                .WithMessage("InjuryNotes is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.InjuryNotes));

            RuleFor(x => x.TrainingHistory)
                .MaximumLength(3000)
                .WithMessage("TrainingHistory is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.TrainingHistory));

            RuleFor(x => x.AvailableDaysPerWeek)
                .MaximumLength(100)
                .WithMessage("AvailableDaysPerWeek is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.AvailableDaysPerWeek));

            RuleFor(x => x.PreferredSessionDurationMinutes)
                .GreaterThan(0)
                .WithMessage("PreferredSessionDurationMinutes must be greater than 0")
                .When(x => x.PreferredSessionDurationMinutes.HasValue);

            RuleFor(x => x.EquipmentAvailable)
                .MaximumLength(500)
                .WithMessage("EquipmentAvailable is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.EquipmentAvailable));
        }
    }
}
