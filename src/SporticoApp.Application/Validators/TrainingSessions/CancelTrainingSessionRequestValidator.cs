using FluentValidation;
using SporticoApp.Application.DTOs.TrainingSessions;

namespace SporticoApp.Application.Validators.TrainingSessions
{
    public class CancelTrainingSessionRequestValidator
        : AbstractValidator<CancelTrainingSessionRequest>
    {
        public CancelTrainingSessionRequestValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(500)
                .WithMessage("Reason is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Reason));
        }
    }
}
