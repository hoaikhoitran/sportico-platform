using FluentValidation;
using SporticoApp.Application.DTOs.CoachPackages;

namespace SporticoApp.Application.Validators.CoachPackages
{
    public class PurchaseCoachPackageRequestValidator
        : AbstractValidator<PurchaseCoachPackageRequest>
    {
        public PurchaseCoachPackageRequestValidator()
        {
            RuleFor(x => x.PackageId)
                .GreaterThan(0)
                .WithMessage("Package id must be greater than 0");
        }
    }
}
