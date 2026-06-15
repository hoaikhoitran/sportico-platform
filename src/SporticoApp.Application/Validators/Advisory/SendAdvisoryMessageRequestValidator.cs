using FluentValidation;
using SporticoApp.Application.DTOs.Advisory;

namespace SporticoApp.Application.Validators.Advisory
{
    public class SendAdvisoryMessageRequestValidator
        : AbstractValidator<SendAdvisoryMessageRequest>
    {
        public SendAdvisoryMessageRequestValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Message is required")
                .MaximumLength(2000)
                .WithMessage("Message is too long");
        }
    }
}
