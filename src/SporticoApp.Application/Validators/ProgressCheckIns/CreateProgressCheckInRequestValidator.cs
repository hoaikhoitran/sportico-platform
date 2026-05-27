using FluentValidation;
using SporticoApp.Application.DTOs.ProgressCheckIns;

namespace SporticoApp.Application.Validators.ProgressCheckIns
{
    public class CreateProgressCheckInRequestValidator
        : AbstractValidator<CreateProgressCheckInRequest>
    {
        public CreateProgressCheckInRequestValidator()
        {
            RuleFor(x => x.EnergyLevel)
                .MaximumLength(50)
                .WithMessage("EnergyLevel is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.EnergyLevel));

            RuleFor(x => x.SleepQuality)
                .MaximumLength(50)
                .WithMessage("SleepQuality is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.SleepQuality));

            RuleFor(x => x.LearnerNote)
                .MaximumLength(2000)
                .WithMessage("LearnerNote is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.LearnerNote));
        }
    }
}
