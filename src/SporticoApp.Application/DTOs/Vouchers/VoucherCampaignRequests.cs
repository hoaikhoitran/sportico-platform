namespace SporticoApp.Application.DTOs.Vouchers
{
    public class CreateVoucherCampaignRequest
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string DiscountType { get; set; } = string.Empty;

        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public int? MaxUsesTotal { get; set; }

        public int? MaxUsesPerLearner { get; set; }

        public decimal? BudgetAmount { get; set; }
    }

    /// <summary>
    /// Financial fields (DiscountType/DiscountValue/MaxDiscountAmount/MinOrderAmount/Code) are only
    /// applied by the service when the campaign has never had a redemption — otherwise they are
    /// silently ignored/rejected to keep already-issued discounts historically accurate; see
    /// VoucherService.UpdateCampaignAsync.
    /// </summary>
    public class UpdateVoucherCampaignRequest
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? DiscountType { get; set; }

        public decimal? DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public int? MaxUsesTotal { get; set; }

        public int? MaxUsesPerLearner { get; set; }

        public decimal? BudgetAmount { get; set; }
    }

    public class VoucherCampaignFilterRequest
    {
        public string? Status { get; set; }

        public string? Keyword { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }

    public class VoucherRedemptionFilterRequest
    {
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
