using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using SporticoApp.Application.DTOs.Packages;

namespace SporticoApp.Application.Validators.Packages
{
    public class CreatePackageRequestValidator : AbstractValidator<CreatePackageRequest>
    {
        public CreatePackageRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(100)
                .WithMessage("Name is too long");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.DurationDays)
                .GreaterThan(0)
                .WithMessage("DurationDays must be greater than 0");

            RuleFor(x => x.MaxPosts)
                .GreaterThan(0)
                .WithMessage("MaxPosts must be greater than 0");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price must be 0 or greater");
        }
    }
}
