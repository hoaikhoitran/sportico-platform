using FluentValidation;
using SporticoApp.Application.DTOs.Vouchers;

namespace SporticoApp.Application.Validators.Vouchers
{
    public class ValidateVoucherRequestValidator : AbstractValidator<ValidateVoucherRequest>
    {
        public ValidateVoucherRequestValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
            RuleFor(x => x.TrainingPackageId).NotEmpty();
        }
    }
}
