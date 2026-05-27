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
        }
    }
}
