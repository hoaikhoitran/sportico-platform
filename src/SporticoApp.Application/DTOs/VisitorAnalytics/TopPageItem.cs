namespace SporticoApp.Application.DTOs.VisitorAnalytics
{
    public class TopPageItem
    {
        public string Path { get; set; } = string.Empty;

        public int ViewCount { get; set; }

        /// <summary>Distinct sessions that viewed this page (a close, single-query proxy for unique visitors).</summary>
        public int UniqueVisitors { get; set; }
    }
}
