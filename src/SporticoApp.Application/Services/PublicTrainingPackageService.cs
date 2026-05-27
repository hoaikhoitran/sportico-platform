using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class PublicTrainingPackageService : IPublicTrainingPackageService
    {
        private readonly ITrainingPackageRepository _trainingPackageRepository;
        private readonly IValidator<TrainingPackageFilterRequest> _filterValidator;

        public PublicTrainingPackageService(
            ITrainingPackageRepository trainingPackageRepository,
            IValidator<TrainingPackageFilterRequest> filterValidator)
        {
            _trainingPackageRepository = trainingPackageRepository;
            _filterValidator = filterValidator;
        }

        public async Task<Result<PagedResult<TrainingPackageResponse>>> GetPagedAsync(
            TrainingPackageFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            filter.Status = TrainingPackageStatuses.Published;

            var (items, totalCount) = await _trainingPackageRepository.GetPagedAsync(filter);

            var response = new PagedResult<TrainingPackageResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<TrainingPackageResponse>>.Success(response);
        }

        public async Task<Result<TrainingPackageResponse>> GetByIdAsync(Guid id)
        {
            var trainingPackage = await _trainingPackageRepository.GetByIdAsync(id);

            if (trainingPackage == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            if (trainingPackage.Status != TrainingPackageStatuses.Published)
            {
                throw new ConflictException(
                    ErrorCodes.TrainingPackageNotPublished,
                    "Training package is not published");
            }

            return Result<TrainingPackageResponse>.Success(trainingPackage.ToResponse());
        }
    }
}
