using FluentValidation;
using SporticoApp.Application.DTOs.Coaches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Validators.Coaches
{
    public class RegisterCoachRequestValidator : AbstractValidator<RegisterCoachRequest>
    {
        public RegisterCoachRequestValidator()
        {
            RuleFor(x => x.Headline)
                .NotEmpty()
                .WithMessage("Headline is required")
                .MinimumLength(5)
                .WithMessage("Headline must be at least 5 characters")
                .MaximumLength(255)
                .WithMessage("Headline is too long");

            RuleFor(x => x.Bio)
                .MaximumLength(2000)
                .WithMessage("Bio is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Bio));

            RuleFor(x => x.ExperienceYears)
                .InclusiveBetween(0, 60)
                .WithMessage("Experience years must be between 0 and 60");

            RuleFor(x => x.SportIds)
                .NotNull()
                .WithMessage("Sport ids are required");

            RuleFor(x => x.SportIds)
                .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
                .WithMessage("Sport ids must not contain duplicates");

            RuleForEach(x => x.SportIds)
                .GreaterThan(0)
                .WithMessage("Sport id must be greater than 0");
        }
    }
}
