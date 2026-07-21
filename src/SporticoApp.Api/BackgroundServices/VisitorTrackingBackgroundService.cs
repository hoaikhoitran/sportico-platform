using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;

namespace SporticoApp.Api.BackgroundServices
{
    /// <summary>
    /// Consumes VisitorTrackingWorkItems off the in-process queue and persists them via a
    /// freshly-scoped IVisitorTrackingService per item — entirely outside any HTTP request's
    /// lifetime, so visitor tracking adds ZERO I/O latency to real requests. A fresh DI scope per
    /// item (not a single long-lived scope) mirrors WithdrawalPayoutReconciliationService: a clean
    /// DbContext/change-tracker per unit of work, and one bad item can never poison the next.
    /// A failure persisting one item is logged and never propagates anywhere — analytics is
    /// best-effort and must never affect business behaviour, and by this point the HTTP response
    /// that triggered the item has already completed.
    /// </summary>
    public class VisitorTrackingBackgroundService : BackgroundService
    {
        private readonly IVisitorTrackingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VisitorTrackingBackgroundService> _logger;

        public VisitorTrackingBackgroundService(
            IVisitorTrackingQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<VisitorTrackingBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var item in _queue.ReadAllAsync(stoppingToken))
                {
                    await ProcessAsync(item, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
        }

        private async Task ProcessAsync(VisitorTrackingWorkItem item, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var trackingService = scope.ServiceProvider.GetRequiredService<IVisitorTrackingService>();

                if (item.Kind == VisitorTrackingWorkItemKind.ApiRequest)
                {
                    await trackingService.TrackApiRequestAsync(
                        item.Context, item.Path, item.Method ?? "GET", item.StatusCode ?? 0);
                }
                else
                {
                    await trackingService.TrackPageViewAsync(item.Context, item.Path, item.Title, item.Referrer);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Visitor tracking background write failed for {Kind} {Path} (request already completed; no impact on it).",
                    item.Kind, item.Path);
            }
        }
    }
}
