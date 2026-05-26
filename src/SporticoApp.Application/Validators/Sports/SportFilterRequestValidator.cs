using FluentValidation;
using SporticoApp.Application.DTOs.Sports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Validators.Sports
{
    public class SportFilterRequestValidator : AbstractValidator<SportFilterRequest>
    {

        public SportFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0.");
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0.");
            RuleFor(x => x.Keyword)
                .MaximumLength(100).WithMessage("Keyword must not exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Keyword));
        }
    }
}
