using FluentValidation;
using SporticoApp.Application.DTOs.Chat;

namespace SporticoApp.Application.Validators.Chat
{
    public class SendMessageRequestValidator
        : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required")
                .MaximumLength(2000)
                .WithMessage("Content is too long");
        }
    }
}
