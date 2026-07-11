using FluentValidation;
using SporticoApp.Application.DTOs.PlatformSettings;

namespace SporticoApp.Application.Validators.PlatformSettings
{
    public class UpdatePlatformCommissionRequestValidator
        : AbstractValidator<UpdatePlatformCommissionRequest>
    {
        public UpdatePlatformCommissionRequestValidator()
        {
            RuleFor(x => x.CommissionPercent)
                .NotNull()
                .WithMessage("CommissionPercent is required")
                .GreaterThanOrEqualTo(0)
                .WithMessage("CommissionPercent must be at least 0")
                .LessThanOrEqualTo(100)
                .WithMessage("CommissionPercent must be at most 100")
                .Must(percent => percent is null || percent.Value % 0.01m == 0m)
                .WithMessage("CommissionPercent supports at most two decimal places");
        }
    }
}
