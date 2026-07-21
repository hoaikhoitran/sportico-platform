namespace SporticoApp.Application.Options
{
    /// <summary>
    /// Visitor-tracking behaviour, bound from the "Analytics" configuration section.
    /// </summary>
    public class AnalyticsOptions
    {
        public const string SectionName = "Analytics";

        /// <summary>
        /// Pepper mixed into the SHA-256 IP hash (see <see cref="SporticoApp.Shared.Helpers.IpHasher"/>)
        /// so raw IPs can never be reconstructed even if the hash leaks. Not an auth secret — set
        /// Analytics__IpHashSalt via environment variable for a deployment-specific value.
        /// </summary>
        public string IpHashSalt { get; set; } = "sportico-analytics-default-pepper-v1";

        /// <summary>When false, the tracking middleware is a no-op (no writes, no cookie).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Minutes of inactivity after which the next hit starts a new session.</summary>
        public int SessionIdleMinutes { get; set; } = 30;

        /// <summary>Rolling window (minutes) used to compute "active visitors right now".</summary>
        public int ActiveWindowMinutes { get; set; } = 5;

        /// <summary>
        /// When false (default), requests whose User-Agent matches a known bot/crawler signature are
        /// not tracked at all (no cookie minted, no session, no metric row) — bot traffic must not
        /// inflate visitor/session counts. Set true to track bots as any other visitor (e.g. to
        /// monitor SEO crawler activity), in which case they still appear with Device="bot".
        /// </summary>
        public bool TrackBots { get; set; } = false;

        /// <summary>Bounded capacity of the in-process tracking queue (see IVisitorTrackingQueue).</summary>
        public int QueueCapacity { get; set; } = 2000;
    }
}
