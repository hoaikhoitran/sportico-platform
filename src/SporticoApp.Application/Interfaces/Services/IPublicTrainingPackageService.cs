using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPublicTrainingPackageService
    {
        Task<Result<PagedResult<PublicTrainingPackageResponse>>> GetPagedAsync(
            TrainingPackageFilterRequest filter);

        Task<Result<PublicTrainingPackageResponse>> GetByIdAsync(Guid id);
    }
}
