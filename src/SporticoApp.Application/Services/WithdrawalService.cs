using FluentValidation;
using SporticoApp.Application.DTOs.Withdrawals;
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

    public class WithdrawalService : IWithdrawalService
    {
        private readonly ICoachRepository _coachRepository;
        private readonly ICoachPayoutAccountRepository _payoutAccountRepository;
        private readonly ICoachWalletRepository _walletRepository;
        private readonly IWithdrawalRequestRepository _withdrawalRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IValidator<CreateWithdrawalRequest> _createValidator;
        private readonly IValidator<WithdrawalRequestFilterRequest> _filterValidator;
        private readonly IValidator<RejectWithdrawalRequest> _rejectValidator;

        public WithdrawalService(
            ICoachRepository coachRepository,
            ICoachPayoutAccountRepository payoutAccountRepository,
            ICoachWalletRepository walletRepository,
            IWithdrawalRequestRepository withdrawalRepository,
            INotificationRepository notificationRepository,
            IValidator<CreateWithdrawalRequest> createValidator,
            IValidator<WithdrawalRequestFilterRequest> filterValidator,
            IValidator<RejectWithdrawalRequest> rejectValidator)
        {
            _coachRepository = coachRepository;
            _payoutAccountRepository = payoutAccountRepository;
            _walletRepository = walletRepository;
            _withdrawalRepository = withdrawalRepository;
            _notificationRepository = notificationRepository;
            _createValidator = createValidator;
            _filterValidator = filterValidator;
            _rejectValidator = rejectValidator;
        }

        public async Task<Result<WithdrawalRequestResponse>> CreateAsync(
            Guid coachId,
            CreateWithdrawalRequest request)
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

            var payoutAccount = await _payoutAccountRepository.GetByCoachIdAsync(coachId);
            if (payoutAccount == null || payoutAccount.Status != PayoutAccountStatuses.Verified)
            {
                throw new ConflictException(
                    ErrorCodes.PayoutAccountRequired,
                    "Verified payout account is required");
            }

            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(coachId);
            if (wallet == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachWalletNotFound,
                    "Coach wallet not found");
            }

            if (request.Amount > wallet.AvailableBalance)
            {
                throw new ConflictException(
                    ErrorCodes.InsufficientWalletBalance,
                    "Insufficient wallet balance");
            }

            wallet.AvailableBalance -= request.Amount;
            wallet.PendingBalance += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var withdrawal = new WithdrawalRequest
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                CoachWalletId = wallet.Id,
                CoachPayoutAccountId = payoutAccount.Id,
                Amount = request.Amount,
                Status = WithdrawalRequestStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _withdrawalRepository.AddWithoutSaveAsync(withdrawal);
            await _withdrawalRepository.SaveChangesAsync();

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        public async Task<Result<PagedResult<WithdrawalRequestResponse>>> GetMyAsync(
            Guid coachId,
            WithdrawalRequestFilterRequest filter)
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

            var (items, totalCount) = await _withdrawalRepository.GetPagedByCoachAsync(coachId, filter);

            var response = new PagedResult<WithdrawalRequestResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<WithdrawalRequestResponse>>.Success(response);
        }

        public async Task<Result<PagedResult<WithdrawalRequestResponse>>> GetPendingAsync(
            WithdrawalRequestFilterRequest filter)
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

            var (items, totalCount) = await _withdrawalRepository.GetPendingPagedAsync(filter);

            var response = new PagedResult<WithdrawalRequestResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<WithdrawalRequestResponse>>.Success(response);
        }

        public async Task<Result<WithdrawalRequestResponse>> ApproveAsync(
            Guid adminId,
            Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            withdrawal.Status = WithdrawalRequestStatuses.Approved;
            withdrawal.ReviewedByUserId = adminId;
            withdrawal.ReviewedAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            await _withdrawalRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.CoachId,
                Title = "Withdrawal approved",
                Content = "Your withdrawal request has been approved",
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        public async Task<Result<WithdrawalRequestResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectWithdrawalRequest request)
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

            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
            if (wallet == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachWalletNotFound,
                    "Coach wallet not found");
            }

            wallet.PendingBalance -= withdrawal.Amount;
            wallet.AvailableBalance += withdrawal.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            withdrawal.Status = WithdrawalRequestStatuses.Rejected;
            withdrawal.AdminNote = request.AdminNote?.Trim();
            withdrawal.ReviewedByUserId = adminId;
            withdrawal.ReviewedAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            await _withdrawalRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.CoachId,
                Title = "Withdrawal rejected",
                Content = request.AdminNote?.Trim() ?? "Your withdrawal request was rejected",
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        public async Task<Result<WithdrawalRequestResponse>> MarkPaidAsync(
            Guid adminId,
            Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
            if (wallet == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachWalletNotFound,
                    "Coach wallet not found");
            }

            wallet.PendingBalance -= withdrawal.Amount;
            wallet.TotalWithdrawn += withdrawal.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            withdrawal.Status = WithdrawalRequestStatuses.Paid;
            withdrawal.ReviewedByUserId = adminId;
            withdrawal.ReviewedAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            await _walletRepository.AddTransactionWithoutSaveAsync(new CoachWalletTransaction
            {
                Id = Guid.NewGuid(),
                CoachWalletId = wallet.Id,
                CoachId = wallet.CoachId,
                Type = WalletTransactionTypes.Withdrawal,
                Direction = WalletTransactionDirections.Debit,
                Amount = withdrawal.Amount,
                ReferenceType = "withdrawal_request",
                ReferenceId = withdrawal.Id,
                CreatedAt = DateTime.UtcNow
            });

            await _withdrawalRepository.SaveChangesAsync();

            await _notificationRepository.AddWithoutSaveAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.CoachId,
                Title = "Withdrawal paid",
                Content = "Your withdrawal request has been paid",
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            });

            await _notificationRepository.SaveChangesAsync();

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }
    }
}
