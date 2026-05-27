using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAdminTrainingPackageService
    {
        Task<Result<PagedResult<TrainingPackageResponse>>> GetPendingAsync(
            TrainingPackageFilterRequest filter);

        Task<Result<TrainingPackageResponse>> ApproveAsync(
            Guid adminId,
            Guid id);

        Task<Result<TrainingPackageResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectTrainingPackageRequest request);
    }
}
