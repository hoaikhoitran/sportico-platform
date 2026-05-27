using FluentValidation;
using SporticoApp.Application.DTOs.PayoutAccounts;
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

    public class CoachPayoutAccountService : ICoachPayoutAccountService
    {
        private readonly ICoachRepository _coachRepository;
        private readonly ICoachPayoutAccountRepository _payoutAccountRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<UpsertCoachPayoutAccountRequest> _upsertValidator;
        private readonly IValidator<RejectCoachPayoutAccountRequest> _rejectValidator;

        public CoachPayoutAccountService(
            ICoachRepository coachRepository,
            ICoachPayoutAccountRepository payoutAccountRepository,
            INotificationRepository notificationRepository,
            IValidator<UpsertCoachPayoutAccountRequest> upsertValidator,
            IValidator<RejectCoachPayoutAccountRequest> rejectValidator)
        {
            _coachRepository = coachRepository;
            _payoutAccountRepository = payoutAccountRepository;
            _notificationRepository = notificationRepository;
            _upsertValidator = upsertValidator;
            _rejectValidator = rejectValidator;
        }

        public async Task<Result<CoachPayoutAccountResponse>> GetMyAsync(Guid coachId)
        {
            var account = await _payoutAccountRepository.GetByCoachIdAsync(coachId);
            if (account == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachPayoutAccountNotFound,
                    "Coach payout account not found");
            }

            return Result<CoachPayoutAccountResponse>.Success(account.ToResponse());
        }

        public async Task<Result<CoachPayoutAccountResponse>> UpsertAsync(
            Guid coachId,
            UpsertCoachPayoutAccountRequest request)
        {
            var validationResult = await _upsertValidator.ValidateAsync(request);
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

            var account = await _payoutAccountRepository.GetByCoachIdForUpdateAsync(coachId);
            if (account == null)
            {
                account = request.ToEntity(coachId);
                await _payoutAccountRepository.AddAsync(account);
            }
            else
            {
                account.ApplyUpdate(request);
                await _payoutAccountRepository.SaveChangesAsync();
            }

            return Result<CoachPayoutAccountResponse>.Success(account.ToResponse());
        }

        public async Task<Result<PagedResult<CoachPayoutAccountResponse>>> GetPendingAsync(
            int pageNumber,
            int pageSize)
        {
            var (items, totalCount) = await _payoutAccountRepository.GetPendingPagedAsync(pageNumber, pageSize);

            var response = new PagedResult<CoachPayoutAccountResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                pageNumber,
                pageSize);

            return Result<PagedResult<CoachPayoutAccountResponse>>.Success(response);
        }

        public async Task<Result<CoachPayoutAccountResponse>> VerifyAsync(
            Guid adminId,
            Guid id)
        {
            var account = await _payoutAccountRepository.GetByIdForUpdateAsync(id);
            if (account == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachPayoutAccountNotFound,
                    "Coach payout account not found");
            }

            account.Status = PayoutAccountStatuses.Verified;
            account.UpdatedAt = DateTime.UtcNow;

            await _payoutAccountRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = account.CoachId,
                Title = "Payout account verified",
                Content = "Your payout account has been verified",
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<CoachPayoutAccountResponse>.Success(account.ToResponse());
        }

        public async Task<Result<CoachPayoutAccountResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectCoachPayoutAccountRequest request)
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

            var account = await _payoutAccountRepository.GetByIdForUpdateAsync(id);
            if (account == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachPayoutAccountNotFound,
                    "Coach payout account not found");
            }

            account.Status = PayoutAccountStatuses.Rejected;
            account.UpdatedAt = DateTime.UtcNow;

            await _payoutAccountRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = account.CoachId,
                Title = "Payout account rejected",
                Content = request.Note?.Trim() ?? "Your payout account was rejected",
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<CoachPayoutAccountResponse>.Success(account.ToResponse());
        }
    }
}
