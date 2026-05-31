using FluentValidation;
using SporticoApp.Application.DTOs.Reviews;

namespace SporticoApp.Application.Validators.Reviews
{
    public class ReviewFilterRequestValidator
        : AbstractValidator<ReviewFilterRequest>
    {
        private static readonly string[] AllowedSorts = { "latest", "highest", "lowest" };

        public ReviewFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50)
                .WithMessage("Page size must be between 1 and 50");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating filter must be between 1 and 5")
                .When(x => x.Rating.HasValue);

            RuleFor(x => x.SortBy)
                .Must(s => AllowedSorts.Contains(s!.Trim().ToLowerInvariant()))
                .WithMessage("SortBy must be one of: latest, highest, lowest")
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));
        }
    }
}
