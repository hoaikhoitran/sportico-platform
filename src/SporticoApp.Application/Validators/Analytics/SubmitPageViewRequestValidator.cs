using FluentValidation;
using SporticoApp.Application.DTOs.Analytics;

namespace SporticoApp.Application.Validators.Analytics
{
    public class SubmitPageViewRequestValidator : AbstractValidator<SubmitPageViewRequest>
    {
        public SubmitPageViewRequestValidator()
        {
            RuleFor(x => x.Path)
                .NotEmpty()
                .WithMessage("Path is required")
                .MaximumLength(500)
                .WithMessage("Path is too long");

            RuleFor(x => x.Title)
                .MaximumLength(200)
                .WithMessage("Title is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Title));

            RuleFor(x => x.Referrer)
                .MaximumLength(500)
                .WithMessage("Referrer is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Referrer));
        }
    }
}
