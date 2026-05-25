using System;
using FluentValidation;
using SporticoApp.Application.DTOs.Sports;

namespace SporticoApp.Application.Validators.Sports
{
    public class CreateSportRequestValidator : AbstractValidator<CreateSportRequest>
    {
        private const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

        public CreateSportRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MinimumLength(2)
                .WithMessage("Name must be at least 2 characters")
                .MaximumLength(100)
                .WithMessage("Name is too long");

            RuleFor(x => x.Slug)
                .MaximumLength(120)
                .WithMessage("Slug is too long")
                .Matches(SlugPattern)
                .WithMessage("Slug format is invalid")
                .When(x => !string.IsNullOrWhiteSpace(x.Slug));

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.IconUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("IconUrl must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.IconUrl));
        }
    }
}
