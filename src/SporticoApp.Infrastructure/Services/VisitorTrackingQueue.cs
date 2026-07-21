using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Analytics;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Options;

namespace SporticoApp.Infrastructure.Services
{
    /// <summary>
    /// In-process producer/consumer buffer decoupling the HTTP request thread (producer, via
    /// TryEnqueue — O(1), no I/O) from VisitorTrackingBackgroundService (sole consumer, does all the
    /// database I/O). Bounded so a stalled consumer can never grow memory unboundedly; DropWrite
    /// makes TryEnqueue non-blocking even when full — analytics is best-effort, so dropping under
    /// extreme load is correct (never backpressure a real request for this).
    /// </summary>
    public class VisitorTrackingQueue : IVisitorTrackingQueue
    {
        private readonly Channel<VisitorTrackingWorkItem> _channel;

        public VisitorTrackingQueue(IOptions<AnalyticsOptions> options)
        {
            var capacity = Math.Max(100, options.Value.QueueCapacity);
            _channel = Channel.CreateBounded<VisitorTrackingWorkItem>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public bool TryEnqueue(VisitorTrackingWorkItem item) => _channel.Writer.TryWrite(item);

        public IAsyncEnumerable<VisitorTrackingWorkItem> ReadAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
