using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ITrainingPackageService
    {
        Task<Result<TrainingPackageResponse>> CreateAsync(
            Guid coachId,
            CreateTrainingPackageRequest request);

        Task<Result<PagedResult<TrainingPackageResponse>>> GetMyPagedAsync(
            Guid coachId,
            TrainingPackageFilterRequest filter);

        Task<Result<TrainingPackageResponse>> GetMyByIdAsync(
            Guid coachId,
            Guid id);

        Task<Result<TrainingPackageResponse>> UpdateAsync(
            Guid coachId,
            Guid id,
            UpdateTrainingPackageRequest request);

        Task<Result<TrainingPackageResponse>> ArchiveAsync(
            Guid coachId,
            Guid id);
    }
}
