using FluentValidation;
using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Application.Validators.Vouchers
{
    public class CreateVoucherCampaignRequestValidator : AbstractValidator<CreateVoucherCampaignRequest>
    {
        public CreateVoucherCampaignRequestValidator()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(64).Matches("^[A-Za-z0-9_-]+$")
                .WithMessage("Code may only contain letters, digits, '-' and '_'.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.DiscountType).NotEmpty().Must(t => VoucherDiscountTypes.All.Contains(t))
                .WithMessage($"discountType must be one of: {string.Join(", ", VoucherDiscountTypes.All)}");
            RuleFor(x => x.DiscountValue).GreaterThan(0);
            RuleFor(x => x.DiscountValue).LessThanOrEqualTo(100)
                .When(x => x.DiscountType == VoucherDiscountTypes.Percentage)
                .WithMessage("Percentage discountValue must be between 0 and 100.");
            RuleFor(x => x.MaxDiscountAmount).GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue);
            RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue);
            RuleFor(x => x.MaxUsesTotal).GreaterThan(0).When(x => x.MaxUsesTotal.HasValue);
            RuleFor(x => x.MaxUsesPerLearner).GreaterThan(0).When(x => x.MaxUsesPerLearner.HasValue);
            RuleFor(x => x.BudgetAmount).GreaterThan(0).When(x => x.BudgetAmount.HasValue);
            RuleFor(x => x)
                .Must(x => !x.StartAt.HasValue || !x.EndAt.HasValue || x.StartAt.Value < x.EndAt.Value)
                .WithMessage("startAt must be before endAt.");
        }
    }

    public class UpdateVoucherCampaignRequestValidator : AbstractValidator<UpdateVoucherCampaignRequest>
    {
        public UpdateVoucherCampaignRequestValidator()
        {
            RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name != null);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.DiscountType).Must(t => t == null || VoucherDiscountTypes.All.Contains(t))
                .WithMessage($"discountType must be one of: {string.Join(", ", VoucherDiscountTypes.All)}");
            RuleFor(x => x.DiscountValue).GreaterThan(0).When(x => x.DiscountValue.HasValue);
            RuleFor(x => x.MaxDiscountAmount).GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue);
            RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue);
            RuleFor(x => x.MaxUsesTotal).GreaterThan(0).When(x => x.MaxUsesTotal.HasValue);
            RuleFor(x => x.MaxUsesPerLearner).GreaterThan(0).When(x => x.MaxUsesPerLearner.HasValue);
            RuleFor(x => x.BudgetAmount).GreaterThan(0).When(x => x.BudgetAmount.HasValue);
            RuleFor(x => x)
                .Must(x => !x.StartAt.HasValue || !x.EndAt.HasValue || x.StartAt.Value < x.EndAt.Value)
                .WithMessage("startAt must be before endAt.");
        }
    }

    public class VoucherCampaignFilterRequestValidator : AbstractValidator<VoucherCampaignFilterRequest>
    {
        public VoucherCampaignFilterRequestValidator()
        {
            RuleFor(x => x.Status).Must(s => s == null || VoucherCampaignStatuses.All.Contains(s));
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    public class VoucherRedemptionFilterRequestValidator : AbstractValidator<VoucherRedemptionFilterRequest>
    {
        public VoucherRedemptionFilterRequestValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
