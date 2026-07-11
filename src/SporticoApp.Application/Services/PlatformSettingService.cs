using FluentValidation;
using SporticoApp.Application.DTOs.PlatformSettings;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class PlatformSettingService : IPlatformSettingService
    {
        private readonly IPlatformSettingRepository _platformSettingRepository;
        private readonly IValidator<UpdatePlatformCommissionRequest> _updateValidator;

        public PlatformSettingService(
            IPlatformSettingRepository platformSettingRepository,
            IValidator<UpdatePlatformCommissionRequest> updateValidator)
        {
            _platformSettingRepository = platformSettingRepository;
            _updateValidator = updateValidator;
        }

        public async Task<Result<PlatformCommissionResponse>> GetCommissionAsync()
        {
            var setting = await _platformSettingRepository.GetOrCreateAsync();

            return Result<PlatformCommissionResponse>.Success(setting.ToCommissionResponse());
        }

        public async Task<Result<PlatformCommissionResponse>> UpdateCommissionAsync(
            Guid adminUserId,
            UpdatePlatformCommissionRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "Invalid request data",
                    details);
            }

            var setting = await _platformSettingRepository.GetOrCreateForUpdateAsync();

            // Percent (admin-facing, 0..100) → fractional rate (persisted, 0..1). This only sets the
            // default for FUTURE bookings; existing bookings keep their snapshotted PlatformFeeRate.
            setting.CommissionRate = request.CommissionPercent!.Value / 100m;
            setting.UpdatedByUserId = adminUserId;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.Version++;

            await _platformSettingRepository.SaveChangesAsync();

            return Result<PlatformCommissionResponse>.Success(setting.ToCommissionResponse());
        }
    }
}
