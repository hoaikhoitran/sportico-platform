using FluentValidation;
using SporticoApp.Application.DTOs.Bookings;

namespace SporticoApp.Application.Validators.Bookings
{
    public class PurchaseTrainingPackagePayOsRequestValidator
        : AbstractValidator<PurchaseTrainingPackagePayOsRequest>
    {
        public PurchaseTrainingPackagePayOsRequestValidator()
        {
            RuleFor(x => x.TrainingPackageId)
                .NotEmpty()
                .WithMessage("TrainingPackageId is required");

            RuleFor(x => x.VoucherCode)
                .MaximumLength(64)
                .When(x => x.VoucherCode != null);
        }
    }
}
