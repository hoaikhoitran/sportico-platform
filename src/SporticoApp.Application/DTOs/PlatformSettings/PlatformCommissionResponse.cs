namespace SporticoApp.Application.DTOs.PlatformSettings
{
    /// <summary>
    /// Admin-facing view of the platform commission. The API speaks percentages (0..100);
    /// the persisted value is a fractional rate (0..1) converted at the Application boundary.
    /// </summary>
    public class PlatformCommissionResponse
    {
        public decimal CommissionPercent { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Guid? UpdatedByUserId { get; set; }
    }
}
