using SporticoApp.Application.DTOs.PlatformSettings;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPlatformSettingService
    {
        Task<Result<PlatformCommissionResponse>> GetCommissionAsync();

        Task<Result<PlatformCommissionResponse>> UpdateCommissionAsync(
            Guid adminUserId,
            UpdatePlatformCommissionRequest request);
    }
}
