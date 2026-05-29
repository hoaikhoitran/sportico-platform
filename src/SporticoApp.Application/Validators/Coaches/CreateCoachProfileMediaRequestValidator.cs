using FluentValidation;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Validators.Coaches
{
    public class CreateCoachProfileMediaRequestValidator : AbstractValidator<CreateCoachProfileMediaRequest>
    {
        public CreateCoachProfileMediaRequestValidator()
        {
            RuleFor(x => x.MediaType)
                .NotEmpty()
                .WithMessage("Media type is required")
                .MaximumLength(50)
                .WithMessage("Media type is too long")
                .Must(CoachProfileMediaTypes.IsValid)
                .WithMessage("Media type must be one of: certificate, award, gallery, identity, other");

            RuleFor(x => x.MediaUrl)
                .NotEmpty()
                .WithMessage("Media URL is required")
                .MaximumLength(1000)
                .WithMessage("Media URL is too long")
                .Must(ValidationRules.BeAValidAbsoluteUrl)
                .WithMessage("Media URL must be a valid absolute URL");

            RuleFor(x => x.Title)
                .MaximumLength(200)
                .WithMessage("Title is too long");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description is too long");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Order index must be greater than or equal to 0");
        }
    }
}
