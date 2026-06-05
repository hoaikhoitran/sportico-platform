using FluentValidation;
using Microsoft.Extensions.Logging;
using SporticoApp.Application.DTOs.TrainingSessions;
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

    public class TrainingSessionService : ITrainingSessionService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITrainingSessionRepository _trainingSessionRepository;
        private readonly ICoachAvailabilityRepository _availabilityRepository;
        private readonly ICoachWalletRepository _coachWalletRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IBookingSessionUsageService _sessionUsageService;
        private readonly ILogger<TrainingSessionService> _logger;
        private readonly IValidator<CreateTrainingSessionRequest> _createValidator;
        private readonly IValidator<ConfirmTrainingSessionRequest> _confirmValidator;
        private readonly IValidator<CancelTrainingSessionRequest> _cancelValidator;
        private readonly IValidator<TrainingSessionFilterRequest> _filterValidator;

        public TrainingSessionService(
            IBookingRepository bookingRepository,
            ITrainingSessionRepository trainingSessionRepository,
            ICoachAvailabilityRepository availabilityRepository,
            ICoachWalletRepository coachWalletRepository,
            INotificationRepository notificationRepository,
            IBookingSessionUsageService sessionUsageService,
            ILogger<TrainingSessionService> logger,
            IValidator<CreateTrainingSessionRequest> createValidator,
            IValidator<ConfirmTrainingSessionRequest> confirmValidator,
            IValidator<CancelTrainingSessionRequest> cancelValidator,
            IValidator<TrainingSessionFilterRequest> filterValidator)
        {
            _bookingRepository = bookingRepository;
            _trainingSessionRepository = trainingSessionRepository;
            _availabilityRepository = availabilityRepository;
            _coachWalletRepository = coachWalletRepository;
            _notificationRepository = notificationRepository;
            _sessionUsageService = sessionUsageService;
            _logger = logger;
            _createValidator = createValidator;
            _confirmValidator = confirmValidator;
            _cancelValidator = cancelValidator;
            _filterValidator = filterValidator;
        }

        /// <summary>
        /// Persists side-effect notifications AFTER the authoritative mutation has already been
        /// committed. A failure here (e.g. an unexpected DB constraint) is logged with context but
        /// never turned into an error response — the business mutation already succeeded.
        /// </summary>
        private async Task SafeNotifyAsync(
            string action,
            Guid? bookingId,
            Guid? sessionId,
            params Notification[] notifications)
        {
            var error = await _notificationRepository.TryAddAndSaveAsync(notifications);
            if (error is not null)
            {
                _logger.LogError(
                    error,
                    "Notification side-effect failed after {Action} (mutation already committed). " +
                    "bookingId={BookingId} sessionId={SessionId} recipients={Recipients} types={Types}",
                    action,
                    bookingId,
                    sessionId,
                    string.Join(",", notifications.Select(n => n.UserId)),
                    string.Join(",", notifications.Select(n => n.Type)));
            }
        }

        public async Task<Result<TrainingSessionResponse>> CreateAsync(
            Guid learnerId,
            CreateTrainingSessionRequest request)
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

            // ── 1. Load and validate the booking ─────────────────────────────────
            var booking = await _bookingRepository.GetByIdForLearnerForUpdateAsync(learnerId, request.BookingId);

            if (booking == null)
            {
                var existing = await _bookingRepository.GetByIdAsync(request.BookingId);
                if (existing != null)
                {
                    throw new ForbiddenException(
                        ErrorCodes.BookingNotOwned,
                        "Booking is not owned by the current learner");
                }

                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            if (booking.Status != BookingStatuses.Active)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Booking is not active");
            }

            // ── 2. Package expiration check ───────────────────────────────────────
            if (booking.ExpiresAt.HasValue && DateTime.UtcNow > booking.ExpiresAt.Value)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Booking package has expired");
            }

            // ── 3. Session-quota check (shared usage rule, scoped strictly by THIS booking) ──
            // Uses the same BookingSessionUsage logic the booking responses use, so the count the
            // learner sees and the count enforced here can never diverge. A learner who buys a new
            // package gets a fresh booking id → fresh usage → not blocked by an old booking.
            var usage = await _sessionUsageService.GetAsync(booking.Id, booking.TotalSessions);

            if (!usage.CanBookSession)
            {
                throw new ConflictException(
                    ErrorCodes.SessionLimitExceeded,
                    "Training session limit exceeded");
            }

            // ── 4. Load and validate the availability slot ────────────────────────
            var slot = await _availabilityRepository.GetByIdForUpdateAsync(request.AvailabilitySlotId);

            if (slot == null)
            {
                throw new NotFoundException(
                    ErrorCodes.ValidationError,
                    "Availability slot not found");
            }

            if (slot.CoachId != booking.CoachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "Availability slot does not belong to the booking's coach");
            }

            if (slot.Status != CoachAvailabilitySlotStatuses.Available)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Availability slot is no longer available");
            }

            if (slot.StartTime <= DateTime.UtcNow)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Availability slot is in the past");
            }

            // Slot must start before the booking expires
            if (booking.ExpiresAt.HasValue && slot.StartTime > booking.ExpiresAt.Value)
            {
                throw new ConflictException(
                    ErrorCodes.BookingNotActive,
                    "Availability slot is after the booking expiration date");
            }

            // ── 5. Overlap safety-guards (secondary to slot-based flow) ───────────
            var activeStatuses = new List<string>
            {
                TrainingSessionStatuses.Requested,
                TrainingSessionStatuses.Scheduled
            };

            var coachOverlap = await _trainingSessionRepository.HasOverlapAsync(
                booking.CoachId,
                slot.StartTime,
                slot.EndTime,
                activeStatuses);

            if (coachOverlap)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Coach has a schedule conflict at this time");
            }

            var learnerOverlap = await _trainingSessionRepository.HasOverlapAsync(
                booking.LearnerId,
                slot.StartTime,
                slot.EndTime,
                activeStatuses);

            if (learnerOverlap)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Learner has a schedule conflict at this time");
            }

            // ── 6. Capacity check (group slots) ───────────────────────────────────
            // A slot can back up to MaxParticipants active sessions. Count the seats already taken;
            // the slot's optimistic concurrency token (bumped below) makes this race-safe so two
            // learners cannot both claim the last seat.
            var activeCount = await _trainingSessionRepository.CountActiveByAvailabilitySlotIdAsync(
                slot.Id,
                TrainingSessionStatuses.CapacityOccupying);

            if (activeCount >= slot.MaxParticipants)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Availability slot is full");
            }

            // ── 7. Create session and update slot capacity state ──────────────────
            var session = request.ToEntity(booking.LearnerId, booking.CoachId, slot);

            // Mark booked only when this booking takes the LAST seat; otherwise keep it available so
            // more learners can still book. For MaxParticipants=1 this reproduces the old behaviour.
            slot.Status = (activeCount + 1 >= slot.MaxParticipants)
                ? CoachAvailabilitySlotStatuses.Booked
                : CoachAvailabilitySlotStatuses.Available;
            slot.UpdatedAt = DateTime.UtcNow;
            slot.Version++; // optimistic concurrency: collide with any concurrent booking of this slot

            // Bump the booking's optimistic concurrency token so two concurrent create-session
            // requests for the SAME booking cannot both pass the quota check and exceed TotalSessions:
            // both read version=N, both write version=N+1 asserting WHERE version=N — the second
            // SaveChanges raises a concurrency conflict (surfaced as 409) and the learner retries
            // against the up-to-date usage.
            booking.Version++;
            booking.UpdatedAt = DateTime.UtcNow;

            // Session insert + slot update + booking version bump share the one scoped DbContext, so
            // this single SaveChanges persists all atomically (and asserts both Version tokens).
            await _trainingSessionRepository.AddAsync(session);

            // Side effect only — must not fail the already-committed session creation.
            await SafeNotifyAsync(
                "CreateTrainingSession",
                booking.Id,
                session.Id,
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = booking.CoachId,
                    Title = "New training session request",
                    Content = "A learner requested a training session",
                    Type = NotificationTypeConstants.TrainingSession,
                    CreatedAt = DateTime.UtcNow
                });

            return Result<TrainingSessionResponse>.Success(session.ToResponse());
        }

        public async Task<Result<PagedResult<TrainingSessionResponse>>> GetByBookingAsync(
            Guid userId,
            Guid bookingId,
            TrainingSessionFilterRequest filter)
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

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null)
            {
                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            if (booking.LearnerId != userId && booking.CoachId != userId)
            {
                throw new ForbiddenException(
                    ErrorCodes.BookingNotOwned,
                    "Booking is not accessible by the current user");
            }

            var (items, totalCount) = await _trainingSessionRepository.GetByBookingPagedAsync(
                bookingId,
                filter);

            var response = new PagedResult<TrainingSessionResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<TrainingSessionResponse>>.Success(response);
        }

        public async Task<Result<TrainingSessionResponse>> ConfirmAsync(
            Guid coachId,
            Guid sessionId,
            ConfirmTrainingSessionRequest request)
        {
            var validationResult = await _confirmValidator.ValidateAsync(request);
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

            var session = await _trainingSessionRepository.GetByIdForUpdateAsync(sessionId);

            if (session == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingSessionNotFound,
                    "Training session not found");
            }

            if (session.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingSessionNotOwned,
                    "Training session is not owned by the current coach");
            }

            if (session.Status != TrainingSessionStatuses.Requested)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Training session status is invalid for confirmation");
            }

            session.Status = TrainingSessionStatuses.Scheduled;
            session.ConfirmedAt = DateTime.UtcNow;
            session.Location = request.Location?.Trim() ?? session.Location;
            session.MeetingUrl = request.MeetingUrl?.Trim() ?? session.MeetingUrl;
            session.CoachNote = request.CoachNote?.Trim();
            session.UpdatedAt = DateTime.UtcNow;

            await _trainingSessionRepository.SaveChangesAsync();

            await SafeNotifyAsync(
                "ConfirmTrainingSession",
                session.BookingId,
                session.Id,
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = session.LearnerId,
                    Title = "Training session confirmed",
                    Content = "Your training session has been confirmed",
                    Type = NotificationTypeConstants.TrainingSession,
                    CreatedAt = DateTime.UtcNow
                });

            return Result<TrainingSessionResponse>.Success(session.ToResponse());
        }

        public async Task<Result<TrainingSessionResponse>> CancelAsync(
            Guid userId,
            Guid sessionId,
            CancelTrainingSessionRequest request)
        {
            var validationResult = await _cancelValidator.ValidateAsync(request);
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

            var session = await _trainingSessionRepository.GetByIdForUpdateAsync(sessionId);

            if (session == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingSessionNotFound,
                    "Training session not found");
            }

            var isCoach = session.CoachId == userId;
            var isLearner = session.LearnerId == userId;

            if (!isCoach && !isLearner)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingSessionNotOwned,
                    "Training session is not accessible by the current user");
            }

            if (session.Status != TrainingSessionStatuses.Requested &&
                session.Status != TrainingSessionStatuses.Scheduled)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Training session status is invalid for cancellation");
            }

            session.Status = TrainingSessionStatuses.Cancelled;
            session.CancelledAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                if (isCoach)
                    session.CoachNote = request.Reason.Trim();
                else
                    session.LearnerNote = request.Reason.Trim();
            }

            // Release a seat on the availability slot in the SAME unit of work as the cancellation
            // so we never end up with "session cancelled but slot still full" (or vice versa).
            // Both entities are tracked by the one scoped DbContext, so a single SaveChanges is atomic.
            if (session.AvailabilitySlotId.HasValue)
            {
                var slot = await _availabilityRepository.GetByIdForUpdateAsync(session.AvailabilitySlotId.Value);
                if (slot != null
                    && slot.Status != CoachAvailabilitySlotStatuses.Cancelled
                    && slot.Status != CoachAvailabilitySlotStatuses.Expired
                    && slot.StartTime > DateTime.UtcNow)
                {
                    // Count the OTHER active sessions (this one is still requested/scheduled in the DB
                    // until the SaveChanges below commits). If a seat opens up, re-open the slot.
                    var remainingActive = await _trainingSessionRepository.CountActiveByAvailabilitySlotIdAsync(
                        slot.Id,
                        TrainingSessionStatuses.CapacityOccupying,
                        excludeSessionId: session.Id);

                    if (remainingActive < slot.MaxParticipants
                        && slot.Status != CoachAvailabilitySlotStatuses.Available)
                    {
                        slot.Status = CoachAvailabilitySlotStatuses.Available;
                        slot.UpdatedAt = DateTime.UtcNow;
                        slot.Version++;
                    }
                }
            }

            await _trainingSessionRepository.SaveChangesAsync();

            // Side effect only — after the cancellation + slot release are committed.
            var notifyUserId = isCoach ? session.LearnerId : session.CoachId;
            await SafeNotifyAsync(
                "CancelTrainingSession",
                session.BookingId,
                session.Id,
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = notifyUserId,
                    Title = "Training session cancelled",
                    Content = "A training session has been cancelled",
                    Type = NotificationTypeConstants.TrainingSession,
                    CreatedAt = DateTime.UtcNow
                });

            return Result<TrainingSessionResponse>.Success(session.ToResponse());
        }

        public async Task<Result<TrainingSessionResponse>> CompleteAsync(
            Guid coachId,
            Guid sessionId)
        {
            var session = await _trainingSessionRepository.GetByIdForUpdateAsync(sessionId);

            if (session == null)
            {
                throw new NotFoundException(
                    ErrorCodes.TrainingSessionNotFound,
                    "Training session not found");
            }

            if (session.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.TrainingSessionNotOwned,
                    "Training session is not owned by the current coach");
            }

            if (session.Status != TrainingSessionStatuses.Scheduled)
            {
                throw new ConflictException(
                    ErrorCodes.InvalidTrainingSessionStatus,
                    "Training session status is invalid for completion");
            }

            session.Status = TrainingSessionStatuses.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            var booking = await _bookingRepository.GetByIdForUpdateAsync(session.BookingId);
            if (booking == null)
            {
                throw new NotFoundException(
                    ErrorCodes.BookingNotFound,
                    "Booking not found");
            }

            // Re-sync the denormalized counter to the REAL completed count rather than blindly
            // incrementing — this self-heals any historical drift and stays consistent with the
            // usage that booking responses report. The DB count excludes this session (not yet
            // saved), so the new authoritative total is (committed completed) + 1.
            var completedInDb = await _trainingSessionRepository.CountByBookingAsync(
                booking.Id,
                new List<string> { TrainingSessionStatuses.Completed });
            booking.CompletedSessions = completedInDb + 1;

            var wallet = await _coachWalletRepository.GetByCoachIdForUpdateAsync(booking.CoachId);
            if (wallet == null)
            {
                wallet = new CoachWallet
                {
                    Id = Guid.NewGuid(),
                    CoachId = booking.CoachId,
                    AvailableBalance = 0,
                    PendingBalance = 0,
                    TotalEarned = 0,
                    TotalWithdrawn = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _coachWalletRepository.AddWithoutSaveAsync(wallet);
            }

            var amount = booking.PerSessionCoachAmount;

            wallet.AvailableBalance += amount;
            wallet.TotalEarned += amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.Version++;

            await _coachWalletRepository.AddTransactionWithoutSaveAsync(new CoachWalletTransaction
            {
                Id = Guid.NewGuid(),
                CoachWalletId = wallet.Id,
                CoachId = wallet.CoachId,
                Type = WalletTransactionTypes.SessionRelease,
                Direction = WalletTransactionDirections.Credit,
                Amount = amount,
                ReferenceType = "training_session",
                ReferenceId = session.Id,
                CreatedAt = DateTime.UtcNow
            });

            if (booking.CompletedSessions >= booking.TotalSessions)
            {
                booking.Status = BookingStatuses.Completed;
                booking.CompletedAt = DateTime.UtcNow;
            }

            // Session completion + booking increment + wallet credit + ledger entry all persist in
            // this one SaveChanges (shared DbContext) → the wallet credit is atomic with completion.
            await _bookingRepository.SaveChangesAsync();

            // Side effects only — after the wallet credit is committed.
            await SafeNotifyAsync(
                "CompleteTrainingSession",
                booking.Id,
                session.Id,
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = booking.LearnerId,
                    Title = "Training session completed",
                    Content = "Your training session has been completed",
                    Type = NotificationTypeConstants.TrainingSession,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = booking.CoachId,
                    Title = "Wallet credited",
                    Content = "Your wallet has been credited for a completed session",
                    Type = NotificationTypeConstants.Wallet,
                    CreatedAt = DateTime.UtcNow
                });

            return Result<TrainingSessionResponse>.Success(session.ToResponse());
        }

        public async Task<Result<PagedResult<TrainingSessionResponse>>> GetMySessionsAsLearnerAsync(
            Guid learnerId,
            TrainingSessionFilterRequest filter)
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

            var (items, totalCount) = await _trainingSessionRepository.GetPagedByLearnerAsync(learnerId, filter);

            var response = new PagedResult<TrainingSessionResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<TrainingSessionResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<TrainingSessionResponse>>> GetMySessionsAsCoachAsync(
            Guid coachId,
            TrainingSessionFilterRequest filter)
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

            var (items, totalCount) = await _trainingSessionRepository.GetPagedByCoachAsync(coachId, filter);

            var response = new PagedResult<TrainingSessionResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<TrainingSessionResponse>>.Success(response);
        }
    }
}
