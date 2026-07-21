using FluentValidation;
using SporticoApp.Application.DTOs.Dashboard;

namespace SporticoApp.Application.Validators.Dashboard
{
    /// <summary>
    /// Additive validator for the existing <see cref="DashboardFilterRequest"/>, following the
    /// same constructor-injected FluentValidation pattern used across the codebase. Does not
    /// replace <c>DashboardService</c>'s own inline range check — that service is untouched.
    /// </summary>
    public class DashboardFilterRequestValidator : AbstractValidator<DashboardFilterRequest>
    {
        public DashboardFilterRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be on or before ToDate");
        }
    }
}
