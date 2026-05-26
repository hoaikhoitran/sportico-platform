using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using SporticoApp.Application.DTOs.Packages;

namespace SporticoApp.Application.Validators.Packages
{
    public class PackageFilterRequestValidator : AbstractValidator<PackageFilterRequest>
    {
        public PackageFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageNumber must be at least 1");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100");

            RuleFor(x => x.Keyword)
                .MaximumLength(100)
                .WithMessage("Keyword is too long")
                .When(x => !string.IsNullOrWhiteSpace(x.Keyword));
        }
    }
}
