using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IVoucherCampaignRepository
    {
        /// <summary>Read-only lookup for the /validate preview — never used to reserve.</summary>
        Task<VoucherCampaign?> GetByCodeAsync(string code);

        /// <summary>Tracked lookup used inside the purchase unit of work (reserve).</summary>
        Task<VoucherCampaign?> GetByCodeForUpdateAsync(string code);

        Task<VoucherCampaign?> GetByIdAsync(Guid id);

        Task<VoucherCampaign?> GetByIdForUpdateAsync(Guid id);

        Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null);

        /// <summary>True if the campaign has ever had ANY redemption (reserved, applied, or released) — financial fields become locked once true.</summary>
        Task<bool> HasAnyRedemptionAsync(Guid campaignId);

        Task<(List<VoucherCampaign> Items, int TotalCount)> GetPagedAsync(VoucherCampaignFilterRequest filter);

        Task AddWithoutSaveAsync(VoucherCampaign campaign);

        Task SaveChangesAsync();
    }
}
