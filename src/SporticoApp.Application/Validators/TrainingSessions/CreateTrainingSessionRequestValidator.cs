using FluentValidation;
using SporticoApp.Application.DTOs.TrainingSessions;

namespace SporticoApp.Application.Validators.TrainingSessions
{
    public class CreateTrainingSessionRequestValidator
        : AbstractValidator<CreateTrainingSessionRequest>
    {
        public CreateTrainingSessionRequestValidator()
        {
            RuleFor(x => x.BookingId)
                .NotEmpty()
                .WithMessage("BookingId is required");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("EndTime must be after StartTime");

            RuleFor(x => x.Location)
                .MaximumLength(255)
                .WithMessage("Location is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Location));

            RuleFor(x => x.MeetingUrl)
                .MaximumLength(1000)
                .WithMessage("MeetingUrl is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));

            RuleFor(x => x.LearnerNote)
                .MaximumLength(2000)
                .WithMessage("LearnerNote is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.LearnerNote));
        }
    }
}
