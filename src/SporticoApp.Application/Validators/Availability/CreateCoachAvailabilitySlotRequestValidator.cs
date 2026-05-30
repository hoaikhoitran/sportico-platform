using FluentValidation;
using SporticoApp.Application.DTOs.Availability;

namespace SporticoApp.Application.Validators.Availability
{
    public class CreateCoachAvailabilitySlotRequestValidator
        : AbstractValidator<CreateCoachAvailabilitySlotRequest>
    {
        public CreateCoachAvailabilitySlotRequestValidator()
        {
            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("StartTime is required");

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .WithMessage("EndTime is required")
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

            RuleFor(x => x.Note)
                .MaximumLength(2000)
                .WithMessage("Note is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Note));
        }
    }
}
