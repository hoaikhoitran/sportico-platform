using FluentValidation;
using SporticoApp.Application.DTOs.Community;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Validators.Community
{
    public class AdminCommunityPostFilterRequestValidator : AbstractValidator<AdminCommunityPostFilterRequest>
    {
        public AdminCommunityPostFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class HideContentRequestValidator : AbstractValidator<HideContentRequest>
    {
        public HideContentRequestValidator()
        {
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        }
    }

    public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
    {
        public CreateReportRequestValidator()
        {
            RuleFor(x => x.TargetType).NotEmpty().Must(t =>
                t == ReportTargetTypes.CommunityPost ||
                t == ReportTargetTypes.CommunityComment ||
                t == ReportTargetTypes.ChatMessage)
                .WithMessage("targetType must be one of: community_post, community_comment, chat_message");
            RuleFor(x => x.TargetId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class ResolveReportRequestValidator : AbstractValidator<ResolveReportRequest>
    {
        public ResolveReportRequestValidator()
        {
            RuleFor(x => x.Status).Must(s => s == ReportStatuses.Resolved || s == ReportStatuses.Rejected)
                .WithMessage("status must be 'resolved' or 'rejected'");
            RuleFor(x => x.ResolutionNote).MaximumLength(1000);
        }
    }
}
