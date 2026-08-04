using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;

namespace SporticoApp.Api.BackgroundServices
{
    /// <summary>
    /// Safety net for PayOS payments/voucher reservations that never got resolved by the webhook or
    /// a learner-triggered reconcile (e.g. the learner simply abandoned the checkout page). Every
    /// mutation here reuses the exact same idempotent, status-guarded release paths as the webhook —
    /// this worker can never release a slot or voucher use more than once, and never touches an
    /// already-applied voucher redemption. A fresh DI scope per tick, zero impact on request latency.
    /// </summary>
    public class PaymentAndVoucherExpirySweepBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);
        private const int BatchSize = 100;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentAndVoucherExpirySweepBackgroundService> _logger;

        public PaymentAndVoucherExpirySweepBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentAndVoucherExpirySweepBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval);

            await TickAsync(stoppingToken);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await TickAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }

        private async Task TickAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // 1) PendingPayment bookings whose PayOS link expired — cancels the booking, releases
                //    the reserved session slots AND (via BookingService -> IVoucherService) the voucher.
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var releasedBookings = await bookingService.ReleaseExpiredPendingPaymentsAsync(BatchSize);

                // 2) Defensive second pass: any voucher redemption still "reserved" past its own
                //    ExpiresAt (should already be covered by #1, but a booking created outside the
                //    normal PayOS flow, or a gap between the two expiry clocks, must not hold a
                //    voucher use forever).
                var voucherService = scope.ServiceProvider.GetRequiredService<IVoucherService>();
                var redemptionRepository = scope.ServiceProvider.GetRequiredService<IVoucherRedemptionRepository>();

                var expiredRedemptions = await redemptionRepository.GetExpiredReservedAsync(DateTime.UtcNow, BatchSize);
                var releasedVouchers = 0;
                foreach (var redemption in expiredRedemptions)
                {
                    await voucherService.ReleaseForBookingAsync(redemption.BookingId, "reservation_expired");
                    releasedVouchers++;
                }

                if (releasedVouchers > 0)
                {
                    await redemptionRepository.SaveChangesAsync();
                }

                if (releasedBookings > 0 || releasedVouchers > 0)
                {
                    _logger.LogInformation(
                        "Payment/voucher expiry sweep: released {Bookings} pending booking(s), {Vouchers} stray voucher reservation(s).",
                        releasedBookings, releasedVouchers);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment/voucher expiry sweep failed; will retry on the next tick.");
            }
        }
    }
}
