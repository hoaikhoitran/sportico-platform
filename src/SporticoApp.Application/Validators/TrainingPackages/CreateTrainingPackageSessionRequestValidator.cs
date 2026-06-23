using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;

namespace SporticoApp.Application.Validators.TrainingPackages
{
    /// <summary>
    /// Per-session field rules. Cross-session rules (count, unique numbering, in-range, no overlap)
    /// live in <see cref="TrainingPackageScheduleValidator"/> and are applied by the package validators.
    /// </summary>
    public class CreateTrainingPackageSessionRequestValidator
        : AbstractValidator<CreateTrainingPackageSessionRequest>
    {
        public CreateTrainingPackageSessionRequestValidator()
        {
            RuleFor(x => x.SessionNumber)
                .GreaterThan(0)
                .WithMessage("SessionNumber must be greater than 0");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("Session StartTime must be before EndTime");

            RuleFor(x => x.MaxParticipants)
                .GreaterThan(0)
                .WithMessage("MaxParticipants must be greater than 0");

            RuleFor(x => x.Location)
                .NotEmpty()
                .WithMessage("Offline sessions must have a location")
                .When(x => !x.IsOnline);

            RuleFor(x => x.Location)
                .MaximumLength(255)
                .WithMessage("Location is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Location));

            RuleFor(x => x.Level)
                .MaximumLength(50)
                .WithMessage("Level is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Level));

            RuleFor(x => x.MeetingUrl)
                .MaximumLength(1000)
                .WithMessage("MeetingUrl is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));

            RuleFor(x => x.Note)
                .MaximumLength(2000)
                .WithMessage("Note is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Note));
        }
    }
}
