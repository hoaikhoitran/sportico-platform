using SporticoApp.Application.DTOs.PlatformSettings;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Mappings
{
    public static class PlatformSettingMappingExtensions
    {
        public static PlatformCommissionResponse ToCommissionResponse(this PlatformSetting setting)
        {
            return new PlatformCommissionResponse
            {
                // Persisted as a fractional rate (0..1); exposed as a percentage (0..100).
                CommissionPercent = setting.CommissionRate * 100m,
                UpdatedAt = setting.UpdatedAt,
                UpdatedByUserId = setting.UpdatedByUserId
            };
        }
    }
}
