using FluentValidation;
using SporticoApp.Application.DTOs.Withdrawals;

namespace SporticoApp.Application.Validators.Withdrawals
{
    public class WithdrawalRequestFilterRequestValidator
        : AbstractValidator<WithdrawalRequestFilterRequest>
    {
        public WithdrawalRequestFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Status)
                .MaximumLength(20)
                .WithMessage("Status is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));
        }
    }
}
