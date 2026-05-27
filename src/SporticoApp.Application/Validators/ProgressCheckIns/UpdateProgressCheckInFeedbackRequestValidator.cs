using FluentValidation;
using SporticoApp.Application.DTOs.ProgressCheckIns;

namespace SporticoApp.Application.Validators.ProgressCheckIns
{
    public class UpdateProgressCheckInFeedbackRequestValidator
        : AbstractValidator<UpdateProgressCheckInFeedbackRequest>
    {
        public UpdateProgressCheckInFeedbackRequestValidator()
        {
            RuleFor(x => x.CoachFeedback)
                .NotEmpty()
                .WithMessage("CoachFeedback is required")
                .MaximumLength(2000)
                .WithMessage("CoachFeedback is too long");
        }
    }
}
