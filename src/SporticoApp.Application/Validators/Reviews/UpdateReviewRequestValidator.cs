using FluentValidation;
using SporticoApp.Application.DTOs.Reviews;

namespace SporticoApp.Application.Validators.Reviews
{
    public class UpdateReviewRequestValidator
        : AbstractValidator<UpdateReviewRequest>
    {
        public UpdateReviewRequestValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5");

            RuleFor(x => x.Comment)
                .MaximumLength(1000)
                .WithMessage("Comment must be at most 1000 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Comment));
        }
    }
}
