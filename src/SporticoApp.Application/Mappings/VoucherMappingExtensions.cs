using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class VoucherMappingExtensions
    {
        public static VoucherCampaignResponse ToResponse(this VoucherCampaign c) => new()
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Description = c.Description,
            DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue,
            MaxDiscountAmount = c.MaxDiscountAmount,
            MinOrderAmount = c.MinOrderAmount,
            StartAt = c.StartAt,
            EndAt = c.EndAt,
            Status = c.Status,
            MaxUsesTotal = c.MaxUsesTotal,
            MaxUsesPerLearner = c.MaxUsesPerLearner,
            ReservedCount = c.ReservedCount,
            UsedCount = c.UsedCount,
            BudgetAmount = c.BudgetAmount,
            ReservedDiscountAmount = c.ReservedDiscountAmount,
            UsedDiscountAmount = c.UsedDiscountAmount,
            CreatedByUserId = c.CreatedByUserId,
            UpdatedByUserId = c.UpdatedByUserId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

        public static VoucherRedemptionResponse ToResponse(this VoucherRedemption r) => new()
        {
            Id = r.Id,
            VoucherCampaignId = r.VoucherCampaignId,
            BookingId = r.BookingId,
            LearnerId = r.LearnerId,
            PaymentId = r.PaymentId,
            Status = r.Status,
            OriginalAmount = r.OriginalAmount,
            DiscountAmount = r.DiscountAmount,
            ReservedAt = r.ReservedAt,
            ExpiresAt = r.ExpiresAt,
            AppliedAt = r.AppliedAt,
            ReleasedAt = r.ReleasedAt,
            ReleaseReason = r.ReleaseReason
        };
    }
}
