using FluentValidation;
using SporticoApp.Application.DTOs.Users;

namespace SporticoApp.Application.Validators.Users
{
    public class AdminCreateUserRequestValidator : AbstractValidator<AdminCreateUserRequest>
    {
        public AdminCreateUserRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email is not valid")
                .MaximumLength(255)
                .WithMessage("Email is too long");

            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required")
                .MinimumLength(2)
                .WithMessage("Full name must be at least 2 characters")
                .MaximumLength(150)
                .WithMessage("Full name is too long");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters")
                .MaximumLength(100)
                .WithMessage("Password is too long");

            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .WithMessage("Phone is too long")
                .Matches(@"^[0-9+\-\s().]+$")
                .WithMessage("Phone may only contain digits and the separators + - ( ) . and spaces")
                .When(x => !string.IsNullOrWhiteSpace(x.Phone));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(1000)
                .WithMessage("Avatar URL is too long")
                .Must(ValidationRules.BeAValidAbsoluteUrlOrEmpty)
                .WithMessage("Avatar URL must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));

            RuleFor(x => x.DateOfBirth)
                .Must(d => !d.HasValue || d.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage("Date of birth cannot be in the future")
                .When(x => x.DateOfBirth.HasValue);

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .MaximumLength(30)
                .WithMessage("Status is too long");

            RuleFor(x => x.Roles)
                .NotEmpty()
                .WithMessage("At least one role is required");

            RuleForEach(x => x.Roles)
                .NotEmpty()
                .WithMessage("Role names must not be empty");
        }
    }
}
