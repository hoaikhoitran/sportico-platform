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
        private readonly ITrainingSessionRepository _trainingSessionRepository;
        private readonly IValidator<CreateCoachAvailabilitySlotRequest> _createValidator;
        private readonly IValidator<CoachAvailabilitySlotFilterRequest> _filterValidator;

        public CoachAvailabilityService(
            ICoachAvailabilityRepository availabilityRepository,
            ICoachRepository coachRepository,
            ITrainingSessionRepository trainingSessionRepository,
            IValidator<CreateCoachAvailabilitySlotRequest> createValidator,
            IValidator<CoachAvailabilitySlotFilterRequest> filterValidator)
        {
            _availabilityRepository = availabilityRepository;
            _coachRepository = coachRepository;
            _trainingSessionRepository = trainingSessionRepository;
            _createValidator = createValidator;
            _filterValidator = filterValidator;
        }

        /// <summary>
        /// Maps a page of slots to responses, batch-loading the active session count per slot so the
        /// capacity fields (booked/remaining/isFull) are accurate rather than assumed zero.
        /// </summary>
        private async Task<List<CoachAvailabilitySlotResponse>> MapWithCapacityAsync(
            IReadOnlyCollection<CoachAvailabilitySlot> slots)
        {
            if (slots.Count == 0)
            {
                return new List<CoachAvailabilitySlotResponse>();
            }

            var counts = await _trainingSessionRepository.CountActiveByAvailabilitySlotIdsAsync(
                slots.Select(s => s.Id).ToList(),
                TrainingSessionStatuses.CapacityOccupying);

            return slots
                .Select(s => s.ToResponse(counts.TryGetValue(s.Id, out var c) ? c : 0))
                .ToList();
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
                // Default to 1 (private slot) when the client omits it — backward compatible.
                MaxParticipants = request.MaxParticipants ?? 1,
                Location = request.Location?.Trim(),
                MeetingUrl = request.MeetingUrl?.Trim(),
                IsOnline = request.IsOnline,
                Note = request.Note?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            await _availabilityRepository.AddAsync(slot);

            // A brand-new slot has no bookings yet.
            return Result<CoachAvailabilitySlotResponse>.Success(slot.ToResponse(bookedParticipants: 0));
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
                await MapWithCapacityAsync(items),
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

            // The repository already filters to status=available && future. Because a slot is flipped
            // to 'booked' only when its last seat is taken, status=available implies remaining>0 — so
            // full slots are excluded. Capacity fields are still populated for display.
            var (items, totalCount) = await _availabilityRepository.GetAvailableByCoachPagedAsync(coachId, filter);

            var response = new PagedResult<CoachAvailabilitySlotResponse>(
                await MapWithCapacityAsync(items),
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

            if (slot.Status == CoachAvailabilitySlotStatuses.Cancelled)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Slot is already cancelled");
            }

            // Option A (safer): block cancelling a group slot that still has active bookings, rather
            // than silently cancelling learners' sessions. A partially-booked slot is status=available
            // (not 'booked'), so a status check alone is insufficient — count active sessions instead.
            var activeBookings = await _trainingSessionRepository.CountActiveByAvailabilitySlotIdAsync(
                slot.Id,
                TrainingSessionStatuses.CapacityOccupying);

            if (activeBookings > 0)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Cannot cancel a slot that has active sessions");
            }

            slot.Status = CoachAvailabilitySlotStatuses.Cancelled;
            slot.UpdatedAt = DateTime.UtcNow;
            slot.Version++;

            await _availabilityRepository.SaveChangesAsync();

            // No active bookings (we just verified), so booked = 0.
            return Result<CoachAvailabilitySlotResponse>.Success(slot.ToResponse(bookedParticipants: 0));
        }
    }
}
