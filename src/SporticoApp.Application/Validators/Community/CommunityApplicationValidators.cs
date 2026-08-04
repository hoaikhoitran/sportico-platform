using FluentValidation;
using SporticoApp.Application.DTOs.Community;

namespace SporticoApp.Application.Validators.Community
{
    public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
    {
        public CreateApplicationRequestValidator()
        {
            RuleFor(x => x.Message).MaximumLength(500);
        }
    }

    public class CommunityApplicationFilterRequestValidator : AbstractValidator<CommunityApplicationFilterRequest>
    {
        public CommunityApplicationFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
