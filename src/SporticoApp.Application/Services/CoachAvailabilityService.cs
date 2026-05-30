using FluentValidation;
using SporticoApp.Application.DTOs.Availability;
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

    public class CoachAvailabilityService : ICoachAvailabilityService
    {
        private readonly ICoachAvailabilityRepository _availabilityRepository;
        private readonly ICoachRepository _coachRepository;
        private readonly IValidator<CreateCoachAvailabilitySlotRequest> _createValidator;
        private readonly IValidator<CoachAvailabilitySlotFilterRequest> _filterValidator;

        public CoachAvailabilityService(
            ICoachAvailabilityRepository availabilityRepository,
            ICoachRepository coachRepository,
            IValidator<CreateCoachAvailabilitySlotRequest> createValidator,
            IValidator<CoachAvailabilitySlotFilterRequest> filterValidator)
        {
            _availabilityRepository = availabilityRepository;
            _coachRepository = coachRepository;
            _createValidator = createValidator;
            _filterValidator = filterValidator;
        }

        public async Task<Result<CoachAvailabilitySlotResponse>> CreateSlotAsync(
            Guid coachId,
            CreateCoachAvailabilitySlotRequest request)
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

            if (request.StartTime <= DateTime.UtcNow)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "StartTime must be in the future");
            }

            var hasOverlap = await _availabilityRepository.HasOverlapAsync(
                coachId, request.StartTime, request.EndTime);

            if (hasOverlap)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "An availability slot already exists in this time range");
            }

            var now = DateTime.UtcNow;
            var slot = new CoachAvailabilitySlot
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = CoachAvailabilitySlotStatuses.Available,
                Location = request.Location?.Trim(),
                MeetingUrl = request.MeetingUrl?.Trim(),
                IsOnline = request.IsOnline,
                Note = request.Note?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _availabilityRepository.AddAsync(slot);

            return Result<CoachAvailabilitySlotResponse>.Success(slot.ToResponse());
        }

        public async Task<Result<PagedResult<CoachAvailabilitySlotResponse>>> GetMySlotsAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter)
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

            var (items, totalCount) = await _availabilityRepository.GetByCoachPagedAsync(coachId, filter);

            var response = new PagedResult<CoachAvailabilitySlotResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<CoachAvailabilitySlotResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<CoachAvailabilitySlotResponse>>> GetCoachPublicSlotsAsync(
            Guid coachId,
            CoachAvailabilitySlotFilterRequest filter)
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

            var coachExists = await _coachRepository.ExistsByUserIdAsync(coachId);
            if (!coachExists)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachProfileNotFound,
                    "Coach not found");
            }

            var (items, totalCount) = await _availabilityRepository.GetAvailableByCoachPagedAsync(coachId, filter);

            var response = new PagedResult<CoachAvailabilitySlotResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<CoachAvailabilitySlotResponse>>.Success(response);
        }

        public async Task<Result<CoachAvailabilitySlotResponse>> CancelSlotAsync(Guid coachId, Guid slotId)
        {
            var slot = await _availabilityRepository.GetByIdForUpdateAsync(slotId);

            if (slot == null)
            {
                throw new NotFoundException(
                    ErrorCodes.ValidationError,
                    "Availability slot not found");
            }

            if (slot.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "Availability slot is not owned by the current coach");
            }

            if (slot.Status == CoachAvailabilitySlotStatuses.Booked)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Cannot cancel a slot that has already been booked");
            }

            if (slot.Status == CoachAvailabilitySlotStatuses.Cancelled)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Slot is already cancelled");
            }

            slot.Status = CoachAvailabilitySlotStatuses.Cancelled;
            slot.UpdatedAt = DateTime.UtcNow;

            await _availabilityRepository.SaveChangesAsync();

            return Result<CoachAvailabilitySlotResponse>.Success(slot.ToResponse());
        }
    }
}
