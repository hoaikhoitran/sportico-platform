using FluentValidation;
using SporticoApp.Application.DTOs.Community;

namespace SporticoApp.Application.Validators.Community
{
    public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
    {
        public CreateCommentRequestValidator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }

    public class CreateReplyRequestValidator : AbstractValidator<CreateReplyRequest>
    {
        public CreateReplyRequestValidator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }

    public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
    {
        public UpdateCommentRequestValidator()
        {
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }

    public class CommunityCommentFilterRequestValidator : AbstractValidator<CommunityCommentFilterRequest>
    {
        public CommunityCommentFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
