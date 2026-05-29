using FluentValidation;
using SporticoApp.Application.DTOs.Auth;

namespace SporticoApp.Application.Validators.Auth
{
    public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
    {
        public ForgotPasswordRequestValidator()
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
