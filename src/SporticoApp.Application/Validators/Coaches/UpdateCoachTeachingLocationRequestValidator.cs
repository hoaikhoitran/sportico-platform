using FluentValidation;
using SporticoApp.Application.DTOs.Coaches;

namespace SporticoApp.Application.Validators.Coaches
{
    public class UpdateCoachTeachingLocationRequestValidator : AbstractValidator<UpdateCoachTeachingLocationRequest>
    {
        public UpdateCoachTeachingLocationRequestValidator()
        {
            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required")
                .MaximumLength(500)
                .WithMessage("Address is too long");

            RuleFor(x => x.City)
                .MaximumLength(100)
                .WithMessage("City is too long");

            RuleFor(x => x.District)
                .MaximumLength(100)
                .WithMessage("District is too long");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90m, 90m)
                .WithMessage("Latitude must be between -90 and 90")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180m, 180m)
                .WithMessage("Longitude must be between -180 and 180")
                .When(x => x.Longitude.HasValue);
        }
    }
}
