using FluentValidation;
using SporticoApp.Application.DTOs.Posts;

namespace SporticoApp.Application.Validators.Posts
{
    public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
    {
        public CreatePostRequestValidator()
        {
            RuleFor(x => x.SportId)
                .GreaterThan(0)
                .WithMessage("SportId must be greater than 0");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title is too long");

            RuleFor(x => x.Description)
                .MaximumLength(3000)
                .WithMessage("Description is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price must be 0 or greater");

            RuleFor(x => x.Location)
                .MaximumLength(255)
                .WithMessage("Location is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Location));

            RuleFor(x => x.ImageUrls)
                .Must(list => list.Count <= 10)
                .WithMessage("ImageUrls must have at most 10 items");

            RuleForEach(x => x.ImageUrls)
                .Must(BeAbsoluteUrl)
                .WithMessage("ImageUrls must be valid absolute URLs")
                .When(x => x.ImageUrls.Count > 0);
        }

        private static bool BeAbsoluteUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
