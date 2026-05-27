using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;

namespace SporticoApp.Application.Validators.TrainingPackages
{
    public class RejectTrainingPackageRequestValidator
        : AbstractValidator<RejectTrainingPackageRequest>
    {
        public RejectTrainingPackageRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required")
                .MaximumLength(1000)
                .WithMessage("Reason is too long");
        }
    }
}
