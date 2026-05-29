using FluentValidation;
using SporticoApp.Application.DTOs.Auth;

namespace SporticoApp.Application.Validators.Auth
{
    public class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
    {
        public ResendVerificationEmailRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email format is invalid")
                .MaximumLength(320)
                .WithMessage("Email is too long");
        }
    }
}
