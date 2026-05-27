using FluentValidation;
using SporticoApp.Application.DTOs.PayoutAccounts;

namespace SporticoApp.Application.Validators.PayoutAccounts
{
    public class UpsertCoachPayoutAccountRequestValidator
        : AbstractValidator<UpsertCoachPayoutAccountRequest>
    {
        public UpsertCoachPayoutAccountRequestValidator()
        {
            RuleFor(x => x.PayoutMethod)
                .NotEmpty()
                .WithMessage("PayoutMethod is required")
                .MaximumLength(50)
                .WithMessage("PayoutMethod is too long");

            RuleFor(x => x.BankName)
                .NotEmpty()
                .WithMessage("BankName is required")
                .MaximumLength(255)
                .WithMessage("BankName is too long");

            RuleFor(x => x.BankAccountNumber)
                .NotEmpty()
                .WithMessage("BankAccountNumber is required")
                .MaximumLength(50)
                .WithMessage("BankAccountNumber is too long");

            RuleFor(x => x.BankAccountHolder)
                .NotEmpty()
                .WithMessage("BankAccountHolder is required")
                .MaximumLength(255)
                .WithMessage("BankAccountHolder is too long");
        }
    }
}
