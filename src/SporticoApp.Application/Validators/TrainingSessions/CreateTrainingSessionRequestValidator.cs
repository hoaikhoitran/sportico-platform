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

            RuleFor(x => x.AvailabilitySlotId)
                .NotEmpty()
                .WithMessage("AvailabilitySlotId is required");

            RuleFor(x => x.LearnerNote)
                .MaximumLength(2000)
                .WithMessage("LearnerNote is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.LearnerNote));
        }
    }
}
