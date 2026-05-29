using FluentValidation;
using SporticoApp.Application.DTOs.Auth;

namespace SporticoApp.Application.Validators.Auth
{
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("Current password is required");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required")
                .MinimumLength(8)
                .WithMessage("New password must be at least 8 characters")
                .MaximumLength(100)
                .WithMessage("New password is too long")
                .NotEqual(x => x.CurrentPassword)
                .WithMessage("New password must be different from the current password");
        }
    }
}
