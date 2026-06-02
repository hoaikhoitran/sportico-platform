using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Notifications;
using SporticoApp.Application.DTOs.Payments;
using SporticoApp.Application.DTOs.Wallets;
using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments;
using SporticoApp.Core.Entities;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using Xunit;

namespace SporticoApp.Application.Tests.Withdrawals;

/// <summary>
/// Covers the coach withdrawal lifecycle and wallet balance invariants:
/// reserve on create, return on reject/fail, release on paid, single debit ledger entry,
/// guarded state transitions, retry re-reservation, and admin status filtering.
/// </summary>
public class WithdrawalServiceTests
{
    private static readonly Guid CoachId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private sealed class Harness
    {
        public WithdrawalService Service = null!;
        public FakeWalletRepo Wallets = null!;
        public FakeWithdrawalRepo Withdrawals = null!;
        public FakePayoutAccountRepo Accounts = null!;
        public FakePayoutService PayOs = null!;
        public CoachWallet Wallet = null!;
    }

    private static Harness Build(
        decimal available = 500m,
        decimal pending = 0m,
        decimal totalWithdrawn = 0m,
        string accountStatus = PayoutAccountStatuses.Verified,
        bool accountExists = true,
        WithdrawalRequest? withdrawal = null,
        IEnumerable<WithdrawalRequest>? all = null)
    {
        var wallet = new CoachWallet
        {
            Id = Guid.NewGuid(),
            CoachId = CoachId,
            AvailableBalance = available,
            PendingBalance = pending,
            TotalWithdrawn = totalWithdrawn
        };

        var account = accountExists
            ? new CoachPayoutAccount
            {
                Id = Guid.NewGuid(),
                CoachId = CoachId,
                Status = accountStatus,
                BankBin = "970418",
                BankAccountNumber = "0123456789",
                BankAccountHolder = "COACH NAME",
                BankName = "BIDV"
            }
            : null;

        var wallets = new FakeWalletRepo(wallet);
        var withdrawals = new FakeWithdrawalRepo(withdrawal, all);
        var accounts = new FakePayoutAccountRepo(account);
        var payos = new FakePayoutService();

        var service = new WithdrawalService(
            new FakeCoachRepo(),
            accounts,
            wallets,
            withdrawals,
            new FakeNotificationRepository(),
            new FakeUserRepo(),
            payos,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WithdrawalService>.Instance,
            Microsoft.Extensions.Options.Options.Create(
                new PayoutOptions { AutoPayoutEnabled = false, PayoutCategory = "salary" }),
            new PassValidator<CreateWithdrawalRequest>(),
            new PassValidator<WithdrawalRequestFilterRequest>(),
            new PassValidator<RejectWithdrawalRequest>());

        return new Harness
        {
            Service = service,
            Wallets = wallets,
            Withdrawals = withdrawals,
            Accounts = accounts,
            PayOs = payos,
            Wallet = wallet
        };
    }

    private static WithdrawalRequest Withdrawal(string status, decimal amount = 100m, string? payoutId = null) => new()
    {
        Id = Guid.NewGuid(),
        CoachId = CoachId,
        CoachWalletId = Guid.NewGuid(),
        CoachPayoutAccountId = Guid.NewGuid(),
        Amount = amount,
        Status = status,
        PayOsPayoutId = payoutId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // 1. Creating a withdrawal requires a verified payout account.
    [Fact]
    public async Task Create_WithoutVerifiedAccount_ThrowsConflict()
    {
        var h = Build(accountStatus: PayoutAccountStatuses.Pending);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Service.CreateAsync(CoachId, new CreateWithdrawalRequest { Amount = 100m }));

        Assert.Equal(ErrorCodes.PayoutAccountRequired, ex.Code);
    }

    // 2. Insufficient balance returns 409.
    [Fact]
    public async Task Create_InsufficientBalance_ThrowsConflict()
    {
        var h = Build(available: 50m);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            h.Service.CreateAsync(CoachId, new CreateWithdrawalRequest { Amount = 100m }));

        Assert.Equal(ErrorCodes.InsufficientWalletBalance, ex.Code);
    }

    // 3. Creating a withdrawal reserves funds: Available → Pending.
    [Fact]
    public async Task Create_ReservesFunds_AvailableToPending()
    {
        var h = Build(available: 500m, pending: 0m);

        var result = await h.Service.CreateAsync(CoachId, new CreateWithdrawalRequest { Amount = 100m });

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, h.Wallet.AvailableBalance);
        Assert.Equal(100m, h.Wallet.PendingBalance);
        Assert.Equal(WithdrawalRequestStatuses.Pending, result.Data!.Status);
    }

    // 4. Rejecting a withdrawal returns funds: Pending → Available.
    [Fact]
    public async Task Reject_ReturnsFunds_PendingToAvailable()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Pending);
        var h = Build(available: 400m, pending: 100m, withdrawal: w);

        var result = await h.Service.RejectAsync(Guid.NewGuid(), w.Id, new RejectWithdrawalRequest { AdminNote = "no" });

        Assert.Equal(WithdrawalRequestStatuses.Rejected, result.Data!.Status);
        Assert.Equal(500m, h.Wallet.AvailableBalance);
        Assert.Equal(0m, h.Wallet.PendingBalance);
    }

    // 5. Marking paid releases the hold: Pending → TotalWithdrawn.
    [Fact]
    public async Task MarkPaid_ReleasesPending_IntoTotalWithdrawn()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Approved);
        var h = Build(available: 400m, pending: 100m, totalWithdrawn: 0m, withdrawal: w);

        var result = await h.Service.MarkPaidAsync(Guid.NewGuid(), w.Id);

        Assert.Equal(WithdrawalRequestStatuses.Paid, result.Data!.Status);
        Assert.Equal(0m, h.Wallet.PendingBalance);
        Assert.Equal(100m, h.Wallet.TotalWithdrawn);
        Assert.Equal(400m, h.Wallet.AvailableBalance); // unchanged — was already deducted at create
    }

    // 6. Marking paid records exactly one debit ledger transaction.
    [Fact]
    public async Task MarkPaid_CreatesExactlyOneDebitTransaction()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Approved);
        var h = Build(available: 400m, pending: 100m, withdrawal: w);

        await h.Service.MarkPaidAsync(Guid.NewGuid(), w.Id);

        var tx = Assert.Single(h.Wallets.Transactions);
        Assert.Equal(WalletTransactionTypes.Withdrawal, tx.Type);
        Assert.Equal(WalletTransactionDirections.Debit, tx.Direction);
        Assert.Equal(100m, tx.Amount);
    }

    // 7. A processing withdrawal cannot be rejected manually.
    [Fact]
    public async Task Reject_ProcessingWithdrawal_ThrowsConflict()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Processing);
        var h = Build(pending: 100m, withdrawal: w);

        await Assert.ThrowsAsync<ConflictException>(() =>
            h.Service.RejectAsync(Guid.NewGuid(), w.Id, new RejectWithdrawalRequest()));
    }

    // 8. A processing withdrawal cannot be marked paid manually.
    [Fact]
    public async Task MarkPaid_ProcessingWithdrawal_ThrowsConflict()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Processing);
        var h = Build(pending: 100m, withdrawal: w);

        await Assert.ThrowsAsync<ConflictException>(() =>
            h.Service.MarkPaidAsync(Guid.NewGuid(), w.Id));
    }

    // 9. Refresh payout status SUCCESS → paid.
    [Fact]
    public async Task RefreshPayoutStatus_Success_MarksPaid()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Processing, payoutId: "po_1");
        var h = Build(available: 400m, pending: 100m, withdrawal: w);
        h.PayOs.DetailState = "SUCCESS";

        var result = await h.Service.RefreshPayoutStatusAsync(w.Id);

        Assert.Equal(WithdrawalRequestStatuses.Paid, result.Data!.Status);
        Assert.Equal(0m, h.Wallet.PendingBalance);
        Assert.Equal(100m, h.Wallet.TotalWithdrawn);
        Assert.Single(h.Wallets.Transactions);
    }

    // 10. Refresh payout status FAILED/CANCELLED/REJECTED → failed and balance returned.
    [Theory]
    [InlineData("FAILED")]
    [InlineData("CANCELLED")]
    [InlineData("REJECTED")]
    public async Task RefreshPayoutStatus_Terminal_FailsAndReturnsBalance(string state)
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Processing, payoutId: "po_1");
        var h = Build(available: 400m, pending: 100m, withdrawal: w);
        h.PayOs.DetailState = state;

        var result = await h.Service.RefreshPayoutStatusAsync(w.Id);

        Assert.Equal(WithdrawalRequestStatuses.Failed, result.Data!.Status);
        Assert.Equal(500m, h.Wallet.AvailableBalance);
        Assert.Equal(0m, h.Wallet.PendingBalance);
        Assert.Empty(h.Wallets.Transactions); // no debit ledger entry on failure
    }

    // 11. Retrying a failed withdrawal re-reserves balance and uses a new idempotency key.
    [Fact]
    public async Task RetryPayout_ReReservesBalance_AndUsesNewIdempotencyKey()
    {
        var w = Withdrawal(WithdrawalRequestStatuses.Failed);
        var h = Build(available: 500m, pending: 0m, withdrawal: w);
        h.PayOs.CreateState = "PROCESSING"; // stays processing, not immediately paid

        var result = await h.Service.RetryPayoutAsync(w.Id);

        Assert.Equal(WithdrawalRequestStatuses.Processing, result.Data!.Status);
        Assert.Equal(400m, h.Wallet.AvailableBalance);
        Assert.Equal(100m, h.Wallet.PendingBalance);

        Assert.NotNull(h.PayOs.LastIdempotencyKey);
        Assert.NotEqual(w.Id.ToString(), h.PayOs.LastIdempotencyKey);
        Assert.Contains("retry", h.PayOs.LastIdempotencyKey!);
    }

    // 12. Admin GetAll can filter by every status.
    [Theory]
    [InlineData(WithdrawalRequestStatuses.Pending, 1)]
    [InlineData(WithdrawalRequestStatuses.Paid, 1)]
    [InlineData(WithdrawalRequestStatuses.Failed, 2)]
    public async Task GetAll_FiltersByStatus(string status, int expected)
    {
        var all = new[]
        {
            Withdrawal(WithdrawalRequestStatuses.Pending),
            Withdrawal(WithdrawalRequestStatuses.Paid),
            Withdrawal(WithdrawalRequestStatuses.Failed),
            Withdrawal(WithdrawalRequestStatuses.Failed)
        };
        var h = Build(all: all);

        var result = await h.Service.GetAllAsync(new WithdrawalRequestFilterRequest { Status = status });

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetAll_NoStatus_ReturnsEverything()
    {
        var all = new[]
        {
            Withdrawal(WithdrawalRequestStatuses.Pending),
            Withdrawal(WithdrawalRequestStatuses.Paid),
            Withdrawal(WithdrawalRequestStatuses.Failed)
        };
        var h = Build(all: all);

        var result = await h.Service.GetAllAsync(new WithdrawalRequestFilterRequest());

        Assert.Equal(3, result.Data!.TotalCount);
    }

    [Fact]
    public async Task GetAll_InvalidStatus_ThrowsValidation()
    {
        var h = Build(all: Array.Empty<WithdrawalRequest>());

        await Assert.ThrowsAsync<ValidationException>(() =>
            h.Service.GetAllAsync(new WithdrawalRequestFilterRequest { Status = "bogus" }));
    }

    // ── fakes ────────────────────────────────────────────────────────────────
    private sealed class FakeCoachRepo : ICoachRepository
    {
        public Task<bool> ExistsByUserIdAsync(Guid userId) => Task.FromResult(true);
        public Task<CoachProfile?> GetByUserIdAsync(Guid userId) => throw new NotImplementedException();
        public Task<CoachProfile?> GetByUserIdWithDetailsAsync(Guid userId) => throw new NotImplementedException();
        public Task<CoachProfile?> GetByUserIdForUpdateAsync(Guid userId) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task CreateCoachProfileAsync(CoachProfile coachProfile, int coachRoleId, List<int> sportIds) => throw new NotImplementedException();
    }

    private sealed class FakePayoutAccountRepo : ICoachPayoutAccountRepository
    {
        private readonly CoachPayoutAccount? _account;
        public FakePayoutAccountRepo(CoachPayoutAccount? account) => _account = account;

        public Task<CoachPayoutAccount?> GetByCoachIdAsync(Guid coachId) => Task.FromResult(_account);
        public Task<CoachPayoutAccount?> GetByCoachIdForUpdateAsync(Guid coachId) => Task.FromResult(_account);
        public Task<CoachPayoutAccount?> GetByIdAsync(Guid id) => Task.FromResult(_account);
        public Task<CoachPayoutAccount?> GetByIdForUpdateAsync(Guid id) => Task.FromResult(_account);
        public Task<(List<CoachPayoutAccount> Items, int TotalCount)> GetPendingPagedAsync(int pageNumber, int pageSize) => throw new NotImplementedException();
        public Task AddAsync(CoachPayoutAccount account) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeWalletRepo : ICoachWalletRepository
    {
        private readonly CoachWallet _wallet;
        public readonly List<CoachWalletTransaction> Transactions = new();

        public FakeWalletRepo(CoachWallet wallet) => _wallet = wallet;

        public Task<CoachWallet?> GetByCoachIdAsync(Guid coachId) => Task.FromResult<CoachWallet?>(_wallet);
        public Task<CoachWallet?> GetByCoachIdForUpdateAsync(Guid coachId) => Task.FromResult<CoachWallet?>(_wallet);
        public Task AddTransactionWithoutSaveAsync(CoachWalletTransaction transaction)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }
        public Task AddAsync(CoachWallet wallet) => Task.CompletedTask;
        public Task AddWithoutSaveAsync(CoachWallet wallet) => Task.CompletedTask;
        public Task<(List<CoachWalletTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(Guid coachId, CoachWalletTransactionFilterRequest filter) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeWithdrawalRepo : IWithdrawalRequestRepository
    {
        private readonly WithdrawalRequest? _single;
        private readonly List<WithdrawalRequest> _all;

        public FakeWithdrawalRepo(WithdrawalRequest? single, IEnumerable<WithdrawalRequest>? all)
        {
            _single = single;
            _all = all?.ToList() ?? new List<WithdrawalRequest>();
        }

        public Task<WithdrawalRequest?> GetByIdAsync(Guid id)
            => Task.FromResult(_single != null && _single.Id == id ? _single : _all.FirstOrDefault(x => x.Id == id));

        public Task<WithdrawalRequest?> GetByIdForUpdateAsync(Guid id)
            => Task.FromResult(_single != null && _single.Id == id ? _single : _all.FirstOrDefault(x => x.Id == id));

        public Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPagedByCoachAsync(Guid coachId, WithdrawalRequestFilterRequest filter)
            => Page(_all.Where(x => x.CoachId == coachId), filter);

        public Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPendingPagedAsync(WithdrawalRequestFilterRequest filter)
            => Page(_all.Where(x => x.Status == WithdrawalRequestStatuses.Pending), filter);

        public Task<(List<WithdrawalRequest> Items, int TotalCount)> GetPagedAsync(WithdrawalRequestFilterRequest filter)
            => Page(_all, filter);

        private static Task<(List<WithdrawalRequest> Items, int TotalCount)> Page(
            IEnumerable<WithdrawalRequest> source, WithdrawalRequestFilterRequest filter)
        {
            var query = source;
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var normalized = filter.Status.Trim().ToLowerInvariant();
                query = query.Where(x => x.Status == normalized);
            }
            var list = query.ToList();
            return Task.FromResult((list, list.Count));
        }

        public Task AddAsync(WithdrawalRequest request) => Task.CompletedTask;
        public Task AddWithoutSaveAsync(WithdrawalRequest request) => Task.CompletedTask;
        public Task SaveChangesAsync() => Task.CompletedTask;
    }

    private sealed class FakeUserRepo : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult<User?>(null);
        public Task<User?> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task<User?> GetByEmailWithRolesAsync(string email) => throw new NotImplementedException();
        public Task AddAsync(User user) => throw new NotImplementedException();
        public Task AddWithoutSaveAsync(User user) => throw new NotImplementedException();
        public Task SaveChangesAsync() => Task.CompletedTask;
        public Task<User?> GetByVerificationTokenAsync(string token) => throw new NotImplementedException();
        public Task<User?> GetByPasswordResetTokenAsync(string token) => throw new NotImplementedException();
        public Task UpdateAsync(User user) => throw new NotImplementedException();
        public Task<User?> GetByIdWithProfilesAndRolesAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
    }

    private sealed class FakePayoutService : IPayOsPayoutService
    {
        public string CreateState = "PROCESSING";
        public string DetailState = "PROCESSING";
        public string? LastIdempotencyKey;

        public Task<PayOsPayoutBalanceResponse> GetBalanceAsync() => throw new NotImplementedException();

        public Task<PayOsCreatePayoutResponse> CreatePayoutAsync(PayOsCreatePayoutRequest request, string idempotencyKey)
        {
            LastIdempotencyKey = idempotencyKey;
            return Task.FromResult(new PayOsCreatePayoutResponse
            {
                Code = "00",
                Desc = "ok",
                Data = new PayOsPayoutData { Id = "po_new", State = CreateState },
                RawJson = "{}"
            });
        }

        public Task<PayOsPayoutDetailResponse> GetPayoutDetailAsync(string payoutId)
            => Task.FromResult(new PayOsPayoutDetailResponse
            {
                Code = "00",
                Desc = "ok",
                Data = new PayOsPayoutData { Id = payoutId, State = DetailState },
                RawJson = "{}"
            });
    }
}
