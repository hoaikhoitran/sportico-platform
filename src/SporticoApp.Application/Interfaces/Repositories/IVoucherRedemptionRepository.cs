using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IVoucherRedemptionRepository
    {
        Task<VoucherRedemption?> GetByBookingIdForUpdateAsync(Guid bookingId);

        Task<int> CountByLearnerAndCampaignAsync(Guid learnerId, Guid campaignId, IReadOnlyCollection<string> statuses);

        Task<(List<VoucherRedemption> Items, int TotalCount)> GetPagedByCampaignAsync(
            Guid campaignId,
            VoucherRedemptionFilterRequest filter);

        /// <summary>Reserved redemptions whose ExpiresAt has passed — for the background release sweep.</summary>
        Task<List<VoucherRedemption>> GetExpiredReservedAsync(DateTime nowUtc, int batchSize);

        Task AddWithoutSaveAsync(VoucherRedemption redemption);

        Task SaveChangesAsync();
    }
}
