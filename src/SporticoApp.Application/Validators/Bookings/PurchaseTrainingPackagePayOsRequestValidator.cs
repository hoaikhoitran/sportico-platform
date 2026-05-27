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
        }
    }
}
