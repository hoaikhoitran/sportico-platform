using FluentValidation;
using SporticoApp.Application.DTOs.TrainingSessions;

namespace SporticoApp.Application.Validators.TrainingSessions
{
    public class ConfirmTrainingSessionRequestValidator
        : AbstractValidator<ConfirmTrainingSessionRequest>
    {
        public ConfirmTrainingSessionRequestValidator()
        {
            RuleFor(x => x.Location)
                .MaximumLength(255)
                .WithMessage("Location is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Location));

            RuleFor(x => x.MeetingUrl)
                .MaximumLength(1000)
                .WithMessage("MeetingUrl is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));

            RuleFor(x => x.CoachNote)
                .MaximumLength(2000)
                .WithMessage("CoachNote is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.CoachNote));
        }
    }
}
