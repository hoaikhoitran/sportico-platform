using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Shared.Constants;

namespace SporticoApp.Api.BackgroundServices
{
    /// <summary>
    /// Periodically flips published/closed community posts whose activity time has passed to
    /// "expired". A fresh DI scope per tick (own DbContext), never affects request latency.
    /// </summary>
    public class CommunityPostExpiryBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
        private const int BatchSize = 200;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CommunityPostExpiryBackgroundService> _logger;

        public CommunityPostExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<CommunityPostExpiryBackgroundService> logger)
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
                var postRepository = scope.ServiceProvider.GetRequiredService<ICommunityPostRepository>();

                var candidates = await postRepository.GetExpiryCandidatesAsync(DateTime.UtcNow, BatchSize);
                if (candidates.Count == 0)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                foreach (var post in candidates)
                {
                    post.Status = CommunityPostStatuses.Expired;
                    post.UpdatedAt = now;
                }

                await postRepository.SaveChangesAsync();

                _logger.LogInformation("Community post expiry sweep expired {Count} post(s).", candidates.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Community post expiry sweep failed; will retry on the next tick.");
            }
        }
    }
}
