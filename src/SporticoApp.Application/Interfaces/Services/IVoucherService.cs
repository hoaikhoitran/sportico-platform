using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IVoucherService
    {
        /// <summary>Learner-facing preview. Read-only — never reserves a seat.</summary>
        Task<Result<VoucherQuoteResponse>> ValidateAsync(Guid learnerId, ValidateVoucherRequest request);

        /// <summary>
        /// Called by BookingService inside the purchase unit of work. Returns null when
        /// <paramref name="voucherCode"/> is null/blank (no voucher requested). Throws the same
        /// business exceptions as <see cref="ValidateAsync"/> when the code is invalid/ineligible.
        /// Mutates the tracked campaign + adds the tracked redemption WITHOUT saving — the caller's
        /// single SaveChangesAsync persists everything atomically together with the booking.
        /// </summary>
        Task<VoucherReservation?> ReserveForBookingAsync(
            Guid learnerId,
            string? voucherCode,
            Guid trainingPackageId,
            decimal originalAmount,
            Guid bookingId);

        /// <summary>Idempotent: no-op unless the booking's redemption is currently "reserved". No save.</summary>
        Task ApplyForBookingAsync(Guid bookingId, Guid? paymentId);

        /// <summary>Idempotent: no-op unless the booking's redemption is currently "reserved" (never releases "applied"). No save.</summary>
        Task ReleaseForBookingAsync(Guid bookingId, string releaseReason);

        // ── Admin campaign management ──────────────────────────────────────────
        Task<Result<VoucherCampaignResponse>> CreateCampaignAsync(Guid adminUserId, CreateVoucherCampaignRequest request);

        Task<Result<VoucherCampaignResponse>> UpdateCampaignAsync(Guid adminUserId, Guid campaignId, UpdateVoucherCampaignRequest request);

        Task<Result<VoucherCampaignResponse>> ActivateCampaignAsync(Guid adminUserId, Guid campaignId);

        Task<Result<VoucherCampaignResponse>> PauseCampaignAsync(Guid adminUserId, Guid campaignId);

        Task<Result<VoucherCampaignResponse>> EndCampaignAsync(Guid adminUserId, Guid campaignId);

        Task<Result<PagedResult<VoucherCampaignResponse>>> GetCampaignsAsync(VoucherCampaignFilterRequest filter);

        Task<Result<VoucherCampaignResponse>> GetCampaignByIdAsync(Guid campaignId);

        Task<Result<PagedResult<VoucherRedemptionResponse>>> GetRedemptionsAsync(Guid campaignId, VoucherRedemptionFilterRequest filter);
    }
}
