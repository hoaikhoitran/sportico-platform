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

    public class TrainingPackageService : ITrainingPackageService
    {
        private readonly ITrainingPackageRepository _trainingPackageRepository;
        private readonly ICoachRepository _coachRepository;
        private readonly ISportRepository _sportRepository;
        private readonly IValidator<CreateTrainingPackageRequest> _createValidator;
        private readonly IValidator<UpdateTrainingPackageRequest> _updateValidator;
        private readonly IValidator<TrainingPackageFilterRequest> _filterValidator;

        public TrainingPackageService(
            ITrainingPackageRepository trainingPackageRepository,
            ICoachRepository coachRepository,
            ISportRepository sportRepository,
            IValidator<CreateTrainingPackageRequest> createValidator,
            IValidator<UpdateTrainingPackageRequest> updateValidator,
            IValidator<TrainingPackageFilterRequest> filterValidator)
        {
            _trainingPackageRepository = trainingPackageRepository;
            _coachRepository = coachRepository;
            _sportRepository = sportRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _filterValidator = filterValidator;
        }

        public async Task<Result<TrainingPackageResponse>> CreateAsync(
            Guid coachId,
            CreateTrainingPackageRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
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

            var coachExists = await _coachRepository.ExistsByUserIdAsync(coachId);
            if (!coachExists)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachProfileRequired,
                    "You must register as a coach first");
            }

            var sport = await _sportRepository.GetByIdAsync(request.SportId);
            if (sport == null || !sport.IsActive)
            {
                throw new ValidationException(
                    ErrorCodes.InvalidSport,
                    "Sport is invalid");
            }

            var entity = request.ToEntity(coachId);

            await _trainingPackageRepository.AddAsync(entity);

            // Assign the (no-tracking) sport only after persisting so EF does not
            // treat it as a new related entity and attempt to re-insert it.
            // ToEntity already sets the SportId FK; this is purely for the response.
            entity.Sport = sport;

            return Result<TrainingPackageResponse>.Success(entity.ToResponse());
        }

        public async Task<Result<PagedResult<TrainingPackageResponse>>> GetMyPagedAsync(
            Guid coachId,
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

            filter.CoachId = coachId;

            var (items, totalCount) = await _trainingPackageRepository.GetPagedAsync(filter);

            var response = new PagedResult<TrainingPackageResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<TrainingPackageResponse>>.Success(response);
        }

        public async Task<Result<TrainingPackageResponse>> GetMyByIdAsync(
            Guid coachId,
            Guid id)
        {
            var trainingPackage = await _trainingPackageRepository.GetOwnedByIdAsync(coachId, id);

            if (trainingPackage == null)
            {
                var existing = await _trainingPackageRepository.GetByIdAsync(id);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.TrainingPackageNotOwned,
                        "Training package is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            return Result<TrainingPackageResponse>.Success(trainingPackage.ToResponse());
        }

        public async Task<Result<TrainingPackageResponse>> UpdateAsync(
            Guid coachId,
            Guid id,
            UpdateTrainingPackageRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
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

            var trainingPackage = await _trainingPackageRepository.GetOwnedByIdForUpdateAsync(coachId, id);

            if (trainingPackage == null)
            {
                var existing = await _trainingPackageRepository.GetByIdAsync(id);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.TrainingPackageNotOwned,
                        "Training package is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            if (trainingPackage.Status != TrainingPackageStatuses.Pending &&
                trainingPackage.Status != TrainingPackageStatuses.Rejected &&
                trainingPackage.Status != TrainingPackageStatuses.Archived)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingPackageStatus,
                    "Training package status is invalid for update");
            }

            var sport = await _sportRepository.GetByIdAsync(request.SportId);
            if (sport == null || !sport.IsActive)
            {
                throw new ValidationException(
                    ErrorCodes.InvalidSport,
                    "Sport is invalid");
            }

            trainingPackage.ApplyUpdate(request);

            await _trainingPackageRepository.SaveChangesAsync();

            var updated = await _trainingPackageRepository.GetOwnedByIdAsync(coachId, id);

            return Result<TrainingPackageResponse>.Success(updated!.ToResponse());
        }

        public async Task<Result<TrainingPackageResponse>> ArchiveAsync(
            Guid coachId,
            Guid id)
        {
            var trainingPackage = await _trainingPackageRepository.GetOwnedByIdForUpdateAsync(coachId, id);

            if (trainingPackage == null)
            {
                var existing = await _trainingPackageRepository.GetByIdAsync(id);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.TrainingPackageNotOwned,
                        "Training package is not owned by the current coach");
                }

                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            trainingPackage.Status = TrainingPackageStatuses.Archived;
            trainingPackage.UpdatedAt = DateTime.UtcNow;

            await _trainingPackageRepository.SaveChangesAsync();

            var updated = await _trainingPackageRepository.GetOwnedByIdAsync(coachId, id);

            return Result<TrainingPackageResponse>.Success(updated!.ToResponse());
        }
    }
}
