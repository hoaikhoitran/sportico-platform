using FluentValidation;
using SporticoApp.Application.DTOs.Reviews;

namespace SporticoApp.Application.Validators.Reviews
{
    public class CreateReviewReportRequestValidator
        : AbstractValidator<CreateReviewReportRequest>
    {
        public CreateReviewReportRequestValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required")
                .MinimumLength(10)
                .WithMessage("Reason must be at least 10 characters")
                .MaximumLength(200)
                .WithMessage("Reason must be at most 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must be at most 1000 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));
        }
    }
}
