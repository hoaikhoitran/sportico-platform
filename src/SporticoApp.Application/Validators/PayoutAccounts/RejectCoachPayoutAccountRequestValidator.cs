using FluentValidation;
using SporticoApp.Application.DTOs.PayoutAccounts;

namespace SporticoApp.Application.Validators.PayoutAccounts
{
    public class RejectCoachPayoutAccountRequestValidator
        : AbstractValidator<RejectCoachPayoutAccountRequest>
    {
        public RejectCoachPayoutAccountRequestValidator()
        {
            RuleFor(x => x.Note)
                .MaximumLength(1000)
                .WithMessage("Note is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Note));
        }
    }
}
