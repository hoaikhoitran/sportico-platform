namespace SporticoApp.Application.DTOs.Vouchers
{
    public class VoucherCampaignResponse
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string DiscountType { get; set; } = string.Empty;

        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public int? MaxUsesTotal { get; set; }

        public int? MaxUsesPerLearner { get; set; }

        public int ReservedCount { get; set; }

        public int UsedCount { get; set; }

        public decimal? BudgetAmount { get; set; }

        public decimal ReservedDiscountAmount { get; set; }

        public decimal UsedDiscountAmount { get; set; }

        public Guid CreatedByUserId { get; set; }

        public Guid? UpdatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class VoucherRedemptionResponse
    {
        public Guid Id { get; set; }

        public Guid VoucherCampaignId { get; set; }

        public Guid BookingId { get; set; }

        public Guid LearnerId { get; set; }

        public Guid? PaymentId { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal OriginalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public DateTime ReservedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime? AppliedAt { get; set; }

        public DateTime? ReleasedAt { get; set; }

        public string? ReleaseReason { get; set; }
    }
}
