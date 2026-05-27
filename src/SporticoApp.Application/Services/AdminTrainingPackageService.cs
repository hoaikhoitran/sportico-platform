using FluentValidation;
using SporticoApp.Application.DTOs.TrainingPackages;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class AdminTrainingPackageService : IAdminTrainingPackageService
    {
        private readonly ITrainingPackageRepository _trainingPackageRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<TrainingPackageFilterRequest> _filterValidator;
        private readonly IValidator<RejectTrainingPackageRequest> _rejectValidator;

        public AdminTrainingPackageService(
            ITrainingPackageRepository trainingPackageRepository,
            INotificationRepository notificationRepository,
            IValidator<TrainingPackageFilterRequest> filterValidator,
            IValidator<RejectTrainingPackageRequest> rejectValidator)
        {
            _trainingPackageRepository = trainingPackageRepository;
            _notificationRepository = notificationRepository;
            _filterValidator = filterValidator;
            _rejectValidator = rejectValidator;
        }

        public async Task<Result<PagedResult<TrainingPackageResponse>>> GetPendingAsync(
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

            filter.Status = TrainingPackageStatuses.Pending;

            var (items, totalCount) = await _trainingPackageRepository.GetPagedAsync(filter);

            var response = new PagedResult<TrainingPackageResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<TrainingPackageResponse>>.Success(response);
        }

        public async Task<Result<TrainingPackageResponse>> ApproveAsync(
            Guid adminId,
            Guid id)
        {
            var trainingPackage = await _trainingPackageRepository.GetByIdForUpdateAsync(id);

            if (trainingPackage == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            if (trainingPackage.Status != TrainingPackageStatuses.Pending)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingPackageStatus,
                    "Training package status is invalid for approval");
            }

            trainingPackage.Status = TrainingPackageStatuses.Published;
            trainingPackage.RejectionReason = null;
            trainingPackage.ReviewedAt = DateTime.UtcNow;
            trainingPackage.ReviewedByUserId = adminId;
            trainingPackage.UpdatedAt = DateTime.UtcNow;

            await _trainingPackageRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = trainingPackage.CoachId,
                Title = "Training package approved",
                Content = "Your training package has been approved",
                Type = NotificationTypeConstants.TrainingPackage,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            var updated = await _trainingPackageRepository.GetByIdAsync(id);

            return Result<TrainingPackageResponse>.Success(updated!.ToResponse());
        }

        public async Task<Result<TrainingPackageResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectTrainingPackageRequest request)
        {
            var validationResult = await _rejectValidator.ValidateAsync(request);
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

            var trainingPackage = await _trainingPackageRepository.GetByIdForUpdateAsync(id);

            if (trainingPackage == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingPackageNotFound,
                    "Training package not found");
            }

            if (trainingPackage.Status != TrainingPackageStatuses.Pending)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingPackageStatus,
                    "Training package status is invalid for rejection");
            }

            trainingPackage.Status = TrainingPackageStatuses.Rejected;
            trainingPackage.RejectionReason = request.Reason.Trim();
            trainingPackage.ReviewedAt = DateTime.UtcNow;
            trainingPackage.ReviewedByUserId = adminId;
            trainingPackage.UpdatedAt = DateTime.UtcNow;

            await _trainingPackageRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = trainingPackage.CoachId,
                Title = "Training package rejected",
                Content = request.Reason.Trim(),
                Type = NotificationTypeConstants.TrainingPackage,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            var updated = await _trainingPackageRepository.GetByIdAsync(id);

            return Result<TrainingPackageResponse>.Success(updated!.ToResponse());
        }
    }
}
