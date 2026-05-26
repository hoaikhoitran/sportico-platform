using SporticoApp.Application.DTOs.CoachPackages;
using SporticoApp.Application.DTOs.Packages;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachPackageService
    {
        Task<Result<PurchaseCoachPackagePayOsResponse>> PurchaseWithPayOsAsync(
            Guid coachId,
            PurchaseCoachPackageRequest request);

        Task<Result<CoachPackageResponse>> PurchaseManualAsync(
            Guid coachId,
            PurchaseCoachPackageRequest request);

        Task<Result<CoachPackageResponse>> GetCurrentAsync(Guid coachId);

        Task<Result<PagedResult<CoachPackageResponse>>> GetHistoryAsync(
            Guid coachId,
            CoachPackageHistoryFilterRequest filter);
    }
}