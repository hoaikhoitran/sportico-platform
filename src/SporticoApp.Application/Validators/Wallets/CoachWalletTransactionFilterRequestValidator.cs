using FluentValidation;
using SporticoApp.Application.DTOs.Wallets;

namespace SporticoApp.Application.Validators.Wallets
{
    public class CoachWalletTransactionFilterRequestValidator
        : AbstractValidator<CoachWalletTransactionFilterRequest>
    {
        public CoachWalletTransactionFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Type)
                .MaximumLength(50)
                .WithMessage("Type is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Type));

            RuleFor(x => x.Direction)
                .MaximumLength(20)
                .WithMessage("Direction is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Direction));
        }
    }
}
