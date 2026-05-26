using FluentValidation;
using SporticoApp.Application.DTOs.Posts;

namespace SporticoApp.Application.Validators.Posts
{
    public class PostFilterRequestValidator : AbstractValidator<PostFilterRequest>
    {
        public PostFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100");

            RuleFor(x => x.Keyword)
                .MaximumLength(100)
                .WithMessage("Keyword is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Keyword));
        }
    }
}
