using FluentValidation;
using SporticoApp.Application.DTOs.Reviews;

namespace SporticoApp.Application.Validators.Reviews
{
    public class ResolveReviewReportRequestValidator
        : AbstractValidator<ResolveReviewReportRequest>
    {
        public ResolveReviewReportRequestValidator()
        {
            RuleFor(x => x.ResolutionNote)
                .MaximumLength(1000)
                .WithMessage("Resolution note must be at most 1000 characters")
                .When(x => !string.IsNullOrWhiteSpace(x.ResolutionNote));
        }
    }
}
