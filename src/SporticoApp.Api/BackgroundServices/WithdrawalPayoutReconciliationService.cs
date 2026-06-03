using Microsoft.Extensions.Options;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;

namespace SporticoApp.Api.BackgroundServices
{
    /// <summary>
    /// Periodically reconciles PayOS payout status for withdrawals that are still <c>processing</c>
    /// with a PayOS payout id. This is what makes a withdrawal eventually transition to <c>paid</c>
    /// (or <c>failed</c>) when PayOS reported <c>PROCESSING</c> at create time and only later settles.
    ///
    /// It reuses <see cref="IWithdrawalService.ReconcileSingleProcessingPayoutAsync"/>, which shares
    /// the exact finalize/rollback logic of the manual refresh-payout-status endpoint, so behaviour
    /// (wallet balances, single debit ledger entry, notifications) is identical.
    /// </summary>
    public class WithdrawalPayoutReconciliationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly WithdrawalPayoutReconciliationOptions _options;
        private readonly ILogger<WithdrawalPayoutReconciliationService> _logger;

        public WithdrawalPayoutReconciliationService(
            IServiceScopeFactory scopeFactory,
            IOptions<WithdrawalPayoutReconciliationOptions> options,
            ILogger<WithdrawalPayoutReconciliationService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation(
                    "Withdrawal payout reconciliation is disabled (WithdrawalPayoutReconciliation:Enabled=false).");
                return;
            }

            // Guard against a misconfigured tiny/zero interval hammering PayOS.
            var interval = TimeSpan.FromSeconds(Math.Max(10, _options.IntervalSeconds));
            var batchSize = Math.Max(1, _options.BatchSize);

            _logger.LogInformation(
                "Withdrawal payout reconciliation started. intervalSeconds={Interval} batchSize={BatchSize}",
                interval.TotalSeconds, batchSize);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(batchSize, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break; // graceful shutdown
                }
                catch (Exception ex)
                {
                    // Never let a pass failure kill the loop.
                    _logger.LogError(ex, "Withdrawal payout reconciliation pass failed.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunOnceAsync(int batchSize, CancellationToken ct)
        {
            // One scope to query the batch (read-only).
            IReadOnlyList<Guid> ids;
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IWithdrawalRequestRepository>();
                ids = await repo.GetProcessingPayoutIdsAsync(batchSize);
            }

            if (ids.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Reconciling {Count} processing payout(s).", ids.Count);

            // A fresh scope per item: clean DbContext + change tracker, isolates failures so one bad
            // withdrawal does not block the rest of the batch.
            foreach (var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IWithdrawalService>();
                    await service.ReconcileSingleProcessingPayoutAsync(id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to reconcile withdrawal {WithdrawalId}; will retry next pass.", id);
                }
            }
        }
    }
}
