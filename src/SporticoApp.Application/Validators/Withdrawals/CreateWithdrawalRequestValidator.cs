using FluentValidation;
using SporticoApp.Application.DTOs.Withdrawals;

namespace SporticoApp.Application.Validators.Withdrawals
{
    public class CreateWithdrawalRequestValidator
        : AbstractValidator<CreateWithdrawalRequest>
    {
        public CreateWithdrawalRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than 0");

            // VND has no minor unit and PayOS payout takes an int amount — reject fractional
            // values rather than silently truncating money.
            RuleFor(x => x.Amount)
                .Must(amount => amount == decimal.Truncate(amount))
                .WithMessage("Amount must be a whole number of VND (no decimals)")
                .When(x => x.Amount > 0);

            // Must fit in the int sent to the PayOS payout API.
            RuleFor(x => x.Amount)
                .LessThanOrEqualTo(int.MaxValue)
                .WithMessage("Amount exceeds the maximum allowed value")
                .When(x => x.Amount > 0);
        }
    }
}
