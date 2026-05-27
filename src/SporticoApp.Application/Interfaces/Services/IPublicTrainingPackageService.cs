using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPublicTrainingPackageService
    {
        Task<Result<PagedResult<TrainingPackageResponse>>> GetPagedAsync(
            TrainingPackageFilterRequest filter);

        Task<Result<TrainingPackageResponse>> GetByIdAsync(Guid id);
    }
}
