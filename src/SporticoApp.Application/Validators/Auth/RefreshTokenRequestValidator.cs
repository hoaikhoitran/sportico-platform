using FluentValidation;
using SporticoApp.Application.DTOs.Auth;

namespace SporticoApp.Application.Validators.Auth
{
    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email format is invalid");

            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("Refresh token is required");
        }
    }
}
