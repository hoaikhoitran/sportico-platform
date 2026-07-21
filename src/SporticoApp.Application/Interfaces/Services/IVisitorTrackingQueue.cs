using SporticoApp.Application.DTOs.Analytics;

namespace SporticoApp.Application.Interfaces.Services
{
    /// <summary>
    /// Non-blocking producer side of the background visitor-tracking pipeline. TryEnqueue is O(1),
    /// never touches the database, and never blocks — the caller (the HTTP request thread) must
    /// never wait on analytics I/O. If the internal buffer is full (the consumer falling behind
    /// under extreme load), the item is silently dropped rather than backpressuring the caller:
    /// visitor analytics is explicitly best-effort and must never slow down or fail a real request.
    /// </summary>
    public interface IVisitorTrackingQueue
    {
        /// <summary>Returns false if the item was dropped because the queue is full.</summary>
        bool TryEnqueue(VisitorTrackingWorkItem item);

        IAsyncEnumerable<VisitorTrackingWorkItem> ReadAllAsync(CancellationToken cancellationToken);
    }
}
