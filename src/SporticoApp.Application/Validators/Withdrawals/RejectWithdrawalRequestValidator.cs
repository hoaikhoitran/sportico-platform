using FluentValidation;
using SporticoApp.Application.DTOs.Withdrawals;

namespace SporticoApp.Application.Validators.Withdrawals
{
    public class RejectWithdrawalRequestValidator
        : AbstractValidator<RejectWithdrawalRequest>
    {
        public RejectWithdrawalRequestValidator()
        {
            RuleFor(x => x.AdminNote)
                .MaximumLength(1000)
                .WithMessage("AdminNote is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.AdminNote));
        }
    }
}
