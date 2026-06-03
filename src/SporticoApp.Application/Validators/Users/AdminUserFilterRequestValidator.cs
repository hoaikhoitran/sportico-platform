using FluentValidation;
using SporticoApp.Application.DTOs.Users;

namespace SporticoApp.Application.Validators.Users
{
    public class AdminUserFilterRequestValidator : AbstractValidator<AdminUserFilterRequest>
    {
        public AdminUserFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");

            RuleFor(x => x.Search)
                .MaximumLength(100)
                .WithMessage("Search is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Search));

            RuleFor(x => x.Role)
                .MaximumLength(30)
                .WithMessage("Role is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Role));

            RuleFor(x => x.Status)
                .MaximumLength(30)
                .WithMessage("Status is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));
        }
    }
}
