using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Interfaces.Repositories
{
    /// <summary>Write-side persistence for visitor tracking (session touch + metric/page-view insert).</summary>
    public interface IVisitorTrackingRepository
    {
        /// <summary>True if any VisitorSession row already exists for this VisitorId (any point in time).</summary>
        Task<bool> HasPriorSessionAsync(Guid visitorId);

        /// <summary>The visitor's session that is still "open" (LastSeenAt within the idle window), if any.</summary>
        Task<VisitorSession?> GetOpenSessionForUpdateAsync(Guid visitorId, DateTime idleSinceUtc);

        Task AddSessionWithoutSaveAsync(VisitorSession session);

        /// <summary>Backend API call — see ApiRequestMetric.</summary>
        Task AddApiRequestMetricWithoutSaveAsync(ApiRequestMetric metric);

        /// <summary>Frontend navigation event — see PageView.</summary>
        Task AddPageViewWithoutSaveAsync(PageView pageView);

        Task SaveChangesAsync();
    }
}
