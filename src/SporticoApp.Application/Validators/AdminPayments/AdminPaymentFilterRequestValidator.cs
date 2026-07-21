using FluentValidation;
using SporticoApp.Application.DTOs.AdminPayments;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Validators.AdminPayments
{
    public class AdminPaymentFilterRequestValidator : AbstractValidator<AdminPaymentFilterRequest>
    {
        private static readonly string[] AllowedStatuses =
        {
            PaymentStatuses.Pending, PaymentStatuses.Paid, PaymentStatuses.Failed, PaymentStatuses.Cancelled
        };

        private static readonly string[] AllowedMethods = { PaymentMethods.PayOs, PaymentMethods.Manual };

        private static readonly string[] AllowedSorts = { "newest", "oldest", "amount_desc", "amount_asc" };

        public AdminPaymentFilterRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be on or before ToDate");

            RuleFor(x => x.Status)
                .Must(s => AllowedStatuses.Contains(s!.Trim().ToLowerInvariant()))
                .WithMessage("Status must be one of: pending, paid, failed, cancelled")
                .When(x => !string.IsNullOrWhiteSpace(x.Status));

            RuleFor(x => x.Method)
                .Must(m => AllowedMethods.Contains(m!.Trim().ToLowerInvariant()))
                .WithMessage("Method must be one of: payos, manual")
                .When(x => !string.IsNullOrWhiteSpace(x.Method));

            RuleFor(x => x.SortBy)
                .Must(s => AllowedSorts.Contains(s!.Trim().ToLowerInvariant()))
                .WithMessage("SortBy must be one of: newest, oldest, amount_desc, amount_asc")
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Page size must be between 1 and 100");
        }
    }
}
