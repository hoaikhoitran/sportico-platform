using FluentValidation;
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
        private readonly ICoachWalletRepository _coachWalletRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CreateTrainingSessionRequest> _createValidator;
        private readonly IValidator<ConfirmTrainingSessionRequest> _confirmValidator;
        private readonly IValidator<CancelTrainingSessionRequest> _cancelValidator;
        private readonly IValidator<TrainingSessionFilterRequest> _filterValidator;

        public TrainingSessionService(
            IBookingRepository bookingRepository,
            ITrainingSessionRepository trainingSessionRepository,
            ICoachWalletRepository coachWalletRepository,
            INotificationRepository notificationRepository,
            IValidator<CreateTrainingSessionRequest> createValidator,
            IValidator<ConfirmTrainingSessionRequest> confirmValidator,
            IValidator<CancelTrainingSessionRequest> cancelValidator,
            IValidator<TrainingSessionFilterRequest> filterValidator)
        {
            _bookingRepository = bookingRepository;
            _trainingSessionRepository = trainingSessionRepository;
            _coachWalletRepository = coachWalletRepository;
            _notificationRepository = notificationRepository;
            _createValidator = createValidator;
            _confirmValidator = confirmValidator;
            _cancelValidator = cancelValidator;
            _filterValidator = filterValidator;
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

            if (request.StartTime <= DateTime.UtcNow)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "StartTime must be in the future");
            }

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

            var countedStatuses = new List<string>
            {
                TrainingSessionStatuses.Requested,
                TrainingSessionStatuses.Scheduled,
                TrainingSessionStatuses.Completed
            };

            var usedSessions = await _trainingSessionRepository.CountByBookingAsync(
                booking.Id,
                countedStatuses);

            if (usedSessions >= booking.TotalSessions)
            {
                throw new ConflictException(
                    ErrorCodes.SessionLimitExceeded,
                    "Training session limit exceeded");
            }

            var activeStatuses = new List<string>
            {
                TrainingSessionStatuses.Requested,
                TrainingSessionStatuses.Scheduled
            };

            var coachOverlap = await _trainingSessionRepository.HasOverlapAsync(
                booking.CoachId,
                request.StartTime,
                request.EndTime,
                activeStatuses);

            if (coachOverlap)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Coach has a schedule conflict");
            }

            var learnerOverlap = await _trainingSessionRepository.HasOverlapAsync(
                booking.LearnerId,
                request.StartTime,
                request.EndTime,
                activeStatuses);

            if (learnerOverlap)
            {
                throw new ConflictException(
                    ErrorCodes.ScheduleConflict,
                    "Learner has a schedule conflict");
            }

            var session = request.ToEntity(booking.LearnerId, booking.CoachId);

            await _trainingSessionRepository.AddAsync(session);

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = booking.CoachId,
                Title = "New training session request",
                Content = "A learner requested a training session",
                Type = NotificationTypeConstants.TrainingSession,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

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

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = session.LearnerId,
                Title = "Training session confirmed",
                Content = "Your training session has been confirmed",
                Type = NotificationTypeConstants.TrainingSession,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

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
                {
                    session.CoachNote = request.Reason.Trim();
                }
                else
                {
                    session.LearnerNote = request.Reason.Trim();
                }
            }

            await _trainingSessionRepository.SaveChangesAsync();

            var notifyUserId = isCoach ? session.LearnerId : session.CoachId;
            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = notifyUserId,
                Title = "Training session cancelled",
                Content = "A training session has been cancelled",
                Type = NotificationTypeConstants.TrainingSession,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

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

            booking.CompletedSessions += 1;

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

            await _bookingRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = booking.LearnerId,
                Title = "Training session completed",
                Content = "Your training session has been completed",
                Type = NotificationTypeConstants.TrainingSession,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = booking.CoachId,
                Title = "Wallet credited",
                Content = "Your wallet has been credited for a completed session",
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<TrainingSessionResponse>.Success(session.ToResponse());
        }
    }
}
