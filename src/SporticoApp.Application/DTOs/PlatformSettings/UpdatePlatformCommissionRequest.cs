namespace SporticoApp.Application.DTOs.PlatformSettings
{
    public class UpdatePlatformCommissionRequest
    {
        /// <summary>
        /// New platform commission as a percentage: 0..100 inclusive, at most two decimal places
        /// (e.g. 0, 7.5, 12.25). Nullable so a missing field fails validation instead of
        /// silently meaning 0%.
        /// </summary>
        public decimal? CommissionPercent { get; set; }
    }
}
