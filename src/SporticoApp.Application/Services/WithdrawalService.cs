using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Application.Options;
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
        private readonly IUserRepository _userRepository;
        private readonly IPayOsPayoutService _payOsPayoutService;
        private readonly ILogger<WithdrawalService> _logger;
        private readonly IValidator<CreateWithdrawalRequest> _createValidator;
        private readonly IValidator<WithdrawalRequestFilterRequest> _filterValidator;
        private readonly IValidator<RejectWithdrawalRequest> _rejectValidator;

        // Injected from PayOsSettings.AutoPayoutEnabled via the caller.
        // Stored as a field so it can be toggled via configuration without redeployment.
        private readonly bool _autoPayoutEnabled;
        private readonly string _payoutCategory;

        // Status values accepted by the admin "all withdrawals" status filter.
        private static readonly HashSet<string> WithdrawalStatusFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            WithdrawalRequestStatuses.Pending,
            WithdrawalRequestStatuses.Approved,
            WithdrawalRequestStatuses.Processing,
            WithdrawalRequestStatuses.Paid,
            WithdrawalRequestStatuses.Rejected,
            WithdrawalRequestStatuses.Failed,
            WithdrawalRequestStatuses.Cancelled
        };

        public WithdrawalService(
            ICoachRepository coachRepository,
            ICoachPayoutAccountRepository payoutAccountRepository,
            ICoachWalletRepository walletRepository,
            IWithdrawalRequestRepository withdrawalRepository,
            INotificationRepository notificationRepository,
            IUserRepository userRepository,
            IPayOsPayoutService payOsPayoutService,
            ILogger<WithdrawalService> logger,
            IOptions<PayoutOptions> payoutOptions,
            IValidator<CreateWithdrawalRequest> createValidator,
            IValidator<WithdrawalRequestFilterRequest> filterValidator,
            IValidator<RejectWithdrawalRequest> rejectValidator)
        {
            _coachRepository = coachRepository;
            _payoutAccountRepository = payoutAccountRepository;
            _walletRepository = walletRepository;
            _withdrawalRepository = withdrawalRepository;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _payOsPayoutService = payOsPayoutService;
            _logger = logger;
            _autoPayoutEnabled = payoutOptions.Value.AutoPayoutEnabled;
            _payoutCategory = payoutOptions.Value.PayoutCategory;
            _createValidator = createValidator;
            _filterValidator = filterValidator;
            _rejectValidator = rejectValidator;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coach: create withdrawal request
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> CreateAsync(
            Guid coachId,
            CreateWithdrawalRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
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
                    "A verified payout account is required");
            }

            // Lock wallet to prevent concurrent double-spend.
            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(coachId);
            if (wallet == null)
            {
                throw new NotFoundException(ErrorCodes.CoachWalletNotFound, "Coach wallet not found");
            }

            if (request.Amount <= 0)
            {
                throw new ValidationException(ErrorCodes.ValidationError, "Amount must be greater than zero");
            }

            if (request.Amount > wallet.AvailableBalance)
            {
                throw new ConflictException(
                    ErrorCodes.InsufficientWalletBalance,
                    "Insufficient wallet balance");
            }

            // ── Reserve funds: AvailableBalance → PendingBalance ─────────────
            // This is NOT an additional platform fee.
            // It is a hold so the coach cannot withdraw the same funds twice.
            // Platform commission (15%) was already deducted at booking purchase.
            wallet.AvailableBalance -= request.Amount;
            wallet.PendingBalance += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.Version++;

            var now = DateTime.UtcNow;

            var withdrawal = new WithdrawalRequest
            {
                Id = Guid.NewGuid(),
                CoachId = coachId,
                CoachWalletId = wallet.Id,
                CoachPayoutAccountId = payoutAccount.Id,
                Amount = request.Amount,
                Status = WithdrawalRequestStatuses.Pending,
                PayOsReferenceId = null, // set after first PayOS call succeeds
                CreatedAt = now,
                UpdatedAt = now
            };

            await _withdrawalRepository.AddWithoutSaveAsync(withdrawal);

            // The repository's SaveChangesAsync re-throws DbUpdateConcurrencyException as
            // ConflictException when the CoachWallet xmin token detects a concurrent write.
            await _withdrawalRepository.SaveChangesAsync();

            // ── Automatic PayOS payout (if enabled) ────────────────────────────
            if (_autoPayoutEnabled)
            {
                await TryInitiatePayOsPayoutAsync(withdrawal, wallet, payoutAccount);
            }

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coach: list own withdrawal requests
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<WithdrawalRequestResponse>>> GetMyAsync(
            Guid coachId,
            WithdrawalRequestFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var (items, totalCount) = await _withdrawalRepository.GetPagedByCoachAsync(coachId, filter);

            var response = new PagedResult<WithdrawalRequestResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<WithdrawalRequestResponse>>.Success(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coach: get one of their own withdrawal requests
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> GetMyByIdAsync(Guid coachId, Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            if (withdrawal.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "You do not have access to this withdrawal");
            }

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: list pending withdrawal requests (kept for backward compatibility)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<WithdrawalRequestResponse>>> GetPendingAsync(
            WithdrawalRequestFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var (items, totalCount) = await _withdrawalRepository.GetPendingPagedAsync(filter);

            var response = new PagedResult<WithdrawalRequestResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<WithdrawalRequestResponse>>.Success(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: list all withdrawal requests, optionally filtered by status
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<PagedResult<WithdrawalRequestResponse>>> GetAllAsync(
            WithdrawalRequestFilterRequest filter)
        {
            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            // Reject unknown status values so callers get a clear 400 instead of an empty page.
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                if (!WithdrawalStatusFilters.Contains(normalized))
                {
                    throw new ValidationException(
                        ErrorCodes.ValidationError,
                        "Invalid withdrawal status filter",
                        new List<string> { $"Allowed statuses: {string.Join(", ", WithdrawalStatusFilters)}" });
                }
            }

            var (items, totalCount) = await _withdrawalRepository.GetPagedAsync(filter);

            var response = new PagedResult<WithdrawalRequestResponse>(
                items.Select(x => x.ToResponse()).ToList(),
                totalCount,
                filter.PageNumber,
                filter.PageSize);

            return Result<PagedResult<WithdrawalRequestResponse>>.Success(response);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: get a single withdrawal request by id (review modals)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> GetByIdAdminAsync(Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: approve (used in manual / non-auto-payout flow)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> ApproveAsync(Guid adminId, Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            // Block approve for any terminal or in-flight status.
            // Rejected and Failed have already returned funds to AvailableBalance;
            // re-approving without re-reserving would corrupt PendingBalance.
            if (withdrawal.Status != WithdrawalRequestStatuses.Pending &&
                withdrawal.Status != WithdrawalRequestStatuses.Approved)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    $"Cannot approve a withdrawal with status '{withdrawal.Status}'");
            }

            withdrawal.Status = WithdrawalRequestStatuses.Approved;
            withdrawal.ReviewedByUserId = adminId;
            withdrawal.ReviewedAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            await _withdrawalRepository.SaveChangesAsync();

            await NotifyCoachAsync(withdrawal,
                "Withdrawal approved",
                "Your withdrawal request has been approved");

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: reject (returns funds to AvailableBalance)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> RejectAsync(
            Guid adminId,
            Guid id,
            RejectWithdrawalRequest request)
        {
            var validationResult = await _rejectValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }

            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            if (withdrawal.Status == WithdrawalRequestStatuses.Processing)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Cannot reject a withdrawal while a PayOS payout is processing. " +
                    "Use the refresh-payout-status endpoint to get the latest state first.");
            }

            if (withdrawal.Status == WithdrawalRequestStatuses.Paid)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Cannot reject an already-paid withdrawal");
            }

            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
            if (wallet == null)
            {
                throw new NotFoundException(ErrorCodes.CoachWalletNotFound, "Coach wallet not found");
            }

            // Return reserved funds to AvailableBalance.
            EnsureSufficientPendingBalance(wallet, withdrawal.Amount);
            wallet.PendingBalance -= withdrawal.Amount;
            wallet.AvailableBalance += withdrawal.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.Version++;

            withdrawal.Status = WithdrawalRequestStatuses.Rejected;
            withdrawal.AdminNote = request.AdminNote?.Trim();
            withdrawal.ReviewedByUserId = adminId;
            withdrawal.ReviewedAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            await _withdrawalRepository.SaveChangesAsync();

            await NotifyCoachAsync(withdrawal,
                "Withdrawal rejected",
                request.AdminNote?.Trim() ?? "Your withdrawal request was rejected");

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: manual mark-paid fallback (safe — blocked while PayOS processing)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> MarkPaidAsync(Guid adminId, Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            // Block manual mark-paid for ANY processing withdrawal — even if PayOsPayoutId is null
            // (e.g. a network-timeout case where PayOS may have already accepted the payout).
            // Allowing mark-paid on a processing withdrawal with a null PayOsPayoutId would create
            // a double-payment risk if PayOS did accept the request but the response was lost.
            if (withdrawal.Status == WithdrawalRequestStatuses.Processing)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "This withdrawal is currently being processed. " +
                    "Use refresh-payout-status to get the latest PayOS result before marking paid manually.");
            }

            if (withdrawal.Status == WithdrawalRequestStatuses.Paid)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Withdrawal is already paid");
            }

            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
            if (wallet == null)
            {
                throw new NotFoundException(ErrorCodes.CoachWalletNotFound, "Coach wallet not found");
            }

            await FinalizeWithdrawalAsPaidAsync(withdrawal, wallet, adminUserId: adminId);

            await NotifyCoachAsync(withdrawal, "Withdrawal paid", "Your withdrawal has been paid");

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: refresh PayOS payout status
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> RefreshPayoutStatusAsync(Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            if (string.IsNullOrWhiteSpace(withdrawal.PayOsPayoutId))
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "No PayOS payout id found on this withdrawal. Cannot refresh status.");
            }

            PayOsPayoutDetailResponse detailResponse;
            try
            {
                detailResponse = await _payOsPayoutService.GetPayoutDetailAsync(withdrawal.PayOsPayoutId);
            }
            catch (Exception ex)
            {
                // Network failure or non-JSON PayOS response — leave in processing state and let
                // admin retry the refresh once PayOS is reachable again.
                withdrawal.FailureReason =
                    $"PayOS detail fetch failed: {ex.Message}. Refresh again when PayOS is reachable.";
                withdrawal.UpdatedAt = DateTime.UtcNow;
                await _withdrawalRepository.SaveChangesAsync();
                return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
            }

            withdrawal.PayOsRawResponse = detailResponse.RawJson;
            withdrawal.PayOsPayoutStatus = detailResponse.Data?.State;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            var state = (detailResponse.Data?.State ?? string.Empty).ToUpperInvariant();

            if (state == "SUCCESS")
            {
                var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
                if (wallet == null)
                    throw new NotFoundException(ErrorCodes.CoachWalletNotFound, "Coach wallet not found");

                await FinalizeWithdrawalAsPaidAsync(withdrawal, wallet, adminUserId: null);
                await NotifyCoachAsync(withdrawal, "Withdrawal paid", "Your withdrawal has been paid");
            }
            else if (state is "FAILED" or "CANCELLED" or "REJECTED")
            {
                var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
                if (wallet == null)
                    throw new NotFoundException(ErrorCodes.CoachWalletNotFound, "Coach wallet not found");

                await RollbackWithdrawalAsync(withdrawal, wallet,
                    $"PayOS payout {state.ToLowerInvariant()}");
            }
            else
            {
                // Still processing
                await _withdrawalRepository.SaveChangesAsync();
            }

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: retry a failed payout
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalRequestResponse>> RetryPayoutAsync(Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdForUpdateAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            if (withdrawal.Status != WithdrawalRequestStatuses.Failed)
            {
                throw new ConflictException(
                    ErrorCodes.ValidationError,
                    "Only failed withdrawals can be retried");
            }

            var payoutAccount = withdrawal.CoachPayoutAccountId.HasValue
                ? await _payoutAccountRepository.GetByIdForUpdateAsync(withdrawal.CoachPayoutAccountId.Value)
                : await _payoutAccountRepository.GetByCoachIdAsync(withdrawal.CoachId);

            if (payoutAccount == null || payoutAccount.Status != PayoutAccountStatuses.Verified)
            {
                throw new ConflictException(
                    ErrorCodes.PayoutAccountRequired,
                    "A verified payout account is required for retry");
            }

            var wallet = await _walletRepository.GetByCoachIdForUpdateAsync(withdrawal.CoachId);
            if (wallet == null)
            {
                throw new NotFoundException(ErrorCodes.CoachWalletNotFound, "Coach wallet not found");
            }

            // Re-reserve funds (they were returned to AvailableBalance on failure).
            if (wallet.AvailableBalance < withdrawal.Amount)
            {
                throw new ConflictException(
                    ErrorCodes.InsufficientWalletBalance,
                    "Insufficient wallet balance to retry payout");
            }

            wallet.AvailableBalance -= withdrawal.Amount;
            wallet.PendingBalance += withdrawal.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.Version++;

            // Use a new idempotency key so PayOS doesn't deduplicate with the previous attempt.
            var retryKey = $"{withdrawal.Id}-retry-{DateTime.UtcNow.Ticks}";

            withdrawal.Status = WithdrawalRequestStatuses.Processing;
            withdrawal.FailureReason = null;
            withdrawal.PayOsPayoutId = null;
            withdrawal.ProcessingAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;
            withdrawal.PayOsReferenceId = retryKey;

            await _withdrawalRepository.SaveChangesAsync();

            await TryInitiatePayOsPayoutAsync(withdrawal, wallet, payoutAccount, idempotencyKey: retryKey);

            return Result<WithdrawalRequestResponse>.Success(withdrawal.ToResponse());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Coach: get withdrawal receipt
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalReceiptResponse>> GetReceiptAsync(Guid coachId, Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            if (withdrawal.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.Forbidden,
                    "You do not have access to this withdrawal");
            }

            return Result<WithdrawalReceiptResponse>.Success(
                await BuildReceiptAsync(withdrawal));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Admin: get withdrawal receipt (all details)
        // ─────────────────────────────────────────────────────────────────────
        public async Task<Result<WithdrawalReceiptResponse>> GetReceiptAdminAsync(Guid id)
        {
            var withdrawal = await _withdrawalRepository.GetByIdAsync(id);
            if (withdrawal == null)
            {
                throw new NotFoundException(
                    ErrorCodes.WithdrawalRequestNotFound,
                    "Withdrawal request not found");
            }

            return Result<WithdrawalReceiptResponse>.Success(
                await BuildReceiptAsync(withdrawal));
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Private helpers
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calls PayOS create payout and updates withdrawal + wallet accordingly.
        /// All database changes are saved inside this method.
        /// </summary>
        private async Task TryInitiatePayOsPayoutAsync(
            WithdrawalRequest withdrawal,
            CoachWallet wallet,
            CoachPayoutAccount payoutAccount,
            string? idempotencyKey = null)
        {
            var key = idempotencyKey ?? withdrawal.Id.ToString();
            withdrawal.PayOsReferenceId = key;

            PayOsCreatePayoutResponse? payosResponse = null;

            try
            {
                payosResponse = await _payOsPayoutService.CreatePayoutAsync(
                    new PayOsCreatePayoutRequest
                    {
                        ReferenceId = key,
                        Amount = (int)withdrawal.Amount,
                        Description = $"SPORTICO WD",
                        ToBin = payoutAccount.BankBin,
                        ToAccountNumber = payoutAccount.BankAccountNumber,
                        Category = _payoutCategory
                    },
                    idempotencyKey: key);
            }
            catch (Exception ex)
            {
                // PayOS API call itself threw (network timeout, DNS failure, etc.).
                // We cannot confirm whether PayOS received the request, so we leave
                // the withdrawal in a safe "processing unknown" state rather than
                // rolling back — a rollback could cause double-payout if PayOS did
                // accept the request but the response was lost.
                withdrawal.Status = WithdrawalRequestStatuses.Processing;
                withdrawal.ProcessingAt = DateTime.UtcNow;
                withdrawal.FailureReason =
                    $"PayOS create payout timed out or failed to connect: {ex.Message}. " +
                    "Use refresh-payout-status to reconcile.";
                withdrawal.UpdatedAt = DateTime.UtcNow;
                await _withdrawalRepository.SaveChangesAsync();
                return;
            }

            withdrawal.PayOsRawResponse = payosResponse.RawJson;
            withdrawal.PayOsPayoutStatus = payosResponse.Data?.State;

            var isSuccess = payosResponse.Code == "00";

            if (!isSuccess)
            {
                // PayOS rejected the payout (invalid BIN, account not found, etc.).
                // Safe to roll back because PayOS confirmed it did not accept the request.
                await RollbackWithdrawalAsync(withdrawal, wallet,
                    $"PayOS rejected payout: {payosResponse.Desc}");
                return;
            }

            var state = (payosResponse.Data?.State ?? string.Empty).ToUpperInvariant();
            withdrawal.PayOsPayoutId = payosResponse.Data?.Id;
            withdrawal.ProcessingAt = DateTime.UtcNow;

            if (state == "SUCCESS")
            {
                await FinalizeWithdrawalAsPaidAsync(withdrawal, wallet, adminUserId: null);
                await NotifyCoachAsync(withdrawal, "Withdrawal paid", "Your withdrawal has been paid");
            }
            else
            {
                // PROCESSING (or unknown state) — keep pending, admin can refresh later.
                withdrawal.Status = WithdrawalRequestStatuses.Processing;
                withdrawal.UpdatedAt = DateTime.UtcNow;
                await _withdrawalRepository.SaveChangesAsync();
                await NotifyCoachAsync(withdrawal,
                    "Withdrawal processing",
                    "Your withdrawal payout is being processed");
            }
        }

        /// <summary>
        /// Marks withdrawal as paid and releases PendingBalance.
        /// AvailableBalance was already decreased at withdrawal creation — do NOT decrease it again.
        /// </summary>
        private async Task FinalizeWithdrawalAsPaidAsync(
            WithdrawalRequest withdrawal,
            CoachWallet wallet,
            Guid? adminUserId)
        {
            if (withdrawal.Status == WithdrawalRequestStatuses.Paid)
            {
                return; // Idempotent — already finalized.
            }

            // Release PendingBalance and record payout debit.
            // AvailableBalance was already decreased at withdrawal creation;
            // do NOT subtract it again here.
            EnsureSufficientPendingBalance(wallet, withdrawal.Amount);
            wallet.PendingBalance -= withdrawal.Amount;
            wallet.TotalWithdrawn += withdrawal.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.Version++;

            withdrawal.Status = WithdrawalRequestStatuses.Paid;
            withdrawal.PaidAt = DateTime.UtcNow;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            if (adminUserId.HasValue)
            {
                withdrawal.ReviewedByUserId = adminUserId;
                withdrawal.ReviewedAt = DateTime.UtcNow;
            }

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
        }

        /// <summary>
        /// Rolls back a failed payout: restores PendingBalance → AvailableBalance.
        /// Does NOT create a withdrawal debit ledger entry.
        /// </summary>
        private async Task RollbackWithdrawalAsync(
            WithdrawalRequest withdrawal,
            CoachWallet wallet,
            string reason)
        {
            if (withdrawal.Status == WithdrawalRequestStatuses.Paid)
            {
                return; // Already paid — do not undo.
            }

            // PayOS payout failed or was rejected, so reserved funds are returned to
            // withdrawable balance.
            EnsureSufficientPendingBalance(wallet, withdrawal.Amount);
            wallet.PendingBalance -= withdrawal.Amount;
            wallet.AvailableBalance += withdrawal.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            wallet.Version++;

            withdrawal.Status = WithdrawalRequestStatuses.Failed;
            withdrawal.FailureReason = reason;
            withdrawal.UpdatedAt = DateTime.UtcNow;

            await _withdrawalRepository.SaveChangesAsync();

            await NotifyCoachAsync(withdrawal,
                "Withdrawal failed",
                $"Your withdrawal could not be processed: {reason}");
        }

        /// <summary>
        /// Guards every PendingBalance subtraction. The invariant is that funds were reserved
        /// (Available → Pending) at creation, so PendingBalance should always cover the amount.
        /// If old/corrupt data or a concurrent update violates this, fail with 409 rather than
        /// writing a negative balance.
        /// </summary>
        private static void EnsureSufficientPendingBalance(CoachWallet wallet, decimal amount)
        {
            if (wallet.PendingBalance < amount)
            {
                throw new ConflictException(
                    ErrorCodes.InsufficientWalletBalance,
                    "Wallet pending balance is lower than the withdrawal amount. " +
                    "Refusing the operation to avoid a negative wallet balance.");
            }
        }

        /// <summary>
        /// Wallet notification raised AFTER the withdrawal/wallet mutation has been committed.
        /// A notification failure is logged with full context but never breaks the already
        /// persisted withdrawal — the main mutation stays authoritative.
        /// </summary>
        private async Task NotifyCoachAsync(WithdrawalRequest withdrawal, string title, string content)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = withdrawal.CoachId,
                Title = title,
                Content = content,
                Type = NotificationTypeConstants.Wallet,
                CreatedAt = DateTime.UtcNow
            };

            var error = await _notificationRepository.TryAddAndSaveAsync(new[] { notification });
            if (error is not null)
            {
                _logger.LogError(
                    error,
                    "Withdrawal notification side-effect failed (withdrawal already committed). " +
                    "action={Action} withdrawalId={WithdrawalId} coachId={CoachId} status={Status} type={Type}",
                    title,
                    withdrawal.Id,
                    withdrawal.CoachId,
                    withdrawal.Status,
                    NotificationTypeConstants.Wallet);
            }
        }

        private async Task<WithdrawalReceiptResponse> BuildReceiptAsync(WithdrawalRequest withdrawal)
        {
            var user = await _userRepository.GetByIdAsync(withdrawal.CoachId);

            CoachPayoutAccount? payoutAccount = null;
            if (withdrawal.CoachPayoutAccountId.HasValue)
            {
                payoutAccount = await _payoutAccountRepository.GetByIdAsync(
                    withdrawal.CoachPayoutAccountId.Value);
            }

            var receiptNumber = $"WD-{withdrawal.CreatedAt:yyyyMMdd}-{withdrawal.Id.ToString()[..8].ToUpperInvariant()}";

            return new WithdrawalReceiptResponse
            {
                ReceiptNumber = receiptNumber,
                WithdrawalRequestId = withdrawal.Id,
                CoachId = withdrawal.CoachId,
                CoachName = user?.FullName ?? user?.Email ?? withdrawal.CoachId.ToString(),
                CoachEmail = user?.Email ?? string.Empty,
                Amount = withdrawal.Amount,
                Currency = "VND",
                Status = withdrawal.Status,
                PayOsPayoutId = withdrawal.PayOsPayoutId,
                PayOsReferenceId = withdrawal.PayOsReferenceId,
                PayOsPayoutStatus = withdrawal.PayOsPayoutStatus,
                FailureReason = withdrawal.FailureReason,
                CreatedAt = withdrawal.CreatedAt,
                ProcessingAt = withdrawal.ProcessingAt,
                PaidAt = withdrawal.PaidAt,
                BankName = payoutAccount?.BankName ?? string.Empty,
                BankBin = payoutAccount?.BankBin ?? string.Empty,
                MaskedAccountNumber = payoutAccount != null
                    ? WithdrawalMappingExtensions.MaskAccountNumber(payoutAccount.BankAccountNumber)
                    : string.Empty,
                AccountHolderName = payoutAccount?.BankAccountHolder ?? string.Empty,
                AdminNote = withdrawal.AdminNote
            };
        }
    }
}
