using FluentValidation;
using SporticoApp.Application.DTOs.Coaches;

namespace SporticoApp.Application.Validators.Coaches
{
    public class UpdateCoachProfileRequestValidator : AbstractValidator<UpdateCoachProfileRequest>
    {
        public UpdateCoachProfileRequestValidator()
        {
            RuleFor(x => x.Headline)
                .MaximumLength(200)
                .WithMessage("Headline is too long");

            RuleFor(x => x.Bio)
                .MaximumLength(3000)
                .WithMessage("Bio is too long");

            RuleFor(x => x.ExperienceYears)
                .InclusiveBetween(0, 80)
                .WithMessage("Experience years must be between 0 and 80")
                .When(x => x.ExperienceYears.HasValue);

            RuleFor(x => x.CoverImageUrl)
                .MaximumLength(1000)
                .WithMessage("Cover image URL is too long")
                .Must(ValidationRules.BeAValidAbsoluteUrlOrEmpty)
                .WithMessage("Cover image URL must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl));

            RuleFor(x => x.TeachingAddress)
                .MaximumLength(500)
                .WithMessage("Teaching address is too long");

            RuleFor(x => x.TeachingCity)
                .MaximumLength(100)
                .WithMessage("Teaching city is too long");

            RuleFor(x => x.TeachingDistrict)
                .MaximumLength(100)
                .WithMessage("Teaching district is too long");

            RuleFor(x => x.TeachingLatitude)
                .InclusiveBetween(-90m, 90m)
                .WithMessage("Teaching latitude must be between -90 and 90")
                .When(x => x.TeachingLatitude.HasValue);

            RuleFor(x => x.TeachingLongitude)
                .InclusiveBetween(-180m, 180m)
                .WithMessage("Teaching longitude must be between -180 and 180")
                .When(x => x.TeachingLongitude.HasValue);

            RuleFor(x => x.Specialties)
                .MaximumLength(1000)
                .WithMessage("Specialties is too long");

            RuleFor(x => x.CertificationsSummary)
                .MaximumLength(2000)
                .WithMessage("Certifications summary is too long");

            RuleFor(x => x.AchievementsSummary)
                .MaximumLength(2000)
                .WithMessage("Achievements summary is too long");

            RuleFor(x => x.FacebookUrl)
                .MaximumLength(1000)
                .WithMessage("Facebook URL is too long")
                .Must(ValidationRules.BeAValidAbsoluteUrlOrEmpty)
                .WithMessage("Facebook URL must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.FacebookUrl));

            RuleFor(x => x.InstagramUrl)
                .MaximumLength(1000)
                .WithMessage("Instagram URL is too long")
                .Must(ValidationRules.BeAValidAbsoluteUrlOrEmpty)
                .WithMessage("Instagram URL must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.InstagramUrl));

            RuleFor(x => x.WebsiteUrl)
                .MaximumLength(1000)
                .WithMessage("Website URL is too long")
                .Must(ValidationRules.BeAValidAbsoluteUrlOrEmpty)
                .WithMessage("Website URL must be a valid absolute URL")
                .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl));
        }
    }
}
