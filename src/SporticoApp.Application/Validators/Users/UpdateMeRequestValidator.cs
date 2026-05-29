using System;
using FluentValidation;
using SporticoApp.Application.DTOs.Users;

namespace SporticoApp.Application.Validators.Users
{
    public class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
    {
        public UpdateMeRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required")
                .MinimumLength(2)
                .WithMessage("Full name must be at least 2 characters")
                .MaximumLength(150)
                .WithMessage("Full name is too long");

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
                .Must(BeNotInTheFuture)
                .WithMessage("Date of birth cannot be in the future")
                .Must(BeReasonableAge)
                .WithMessage("Date of birth is not reasonable (older than 120 years)")
                .When(x => x.DateOfBirth.HasValue);
        }

        private static bool BeNotInTheFuture(DateTime? dateOfBirth)
        {
            return !dateOfBirth.HasValue || dateOfBirth.Value.Date <= DateTime.UtcNow.Date;
        }

        private static bool BeReasonableAge(DateTime? dateOfBirth)
        {
            return !dateOfBirth.HasValue || dateOfBirth.Value.Date >= DateTime.UtcNow.Date.AddYears(-120);
        }
    }
}
