using FluentValidation;
using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class CoachTeachingLocationService : ICoachTeachingLocationService
    {
        private readonly ICoachTeachingLocationRepository _locationRepository;
        private readonly ICoachRepository _coachRepository;
        private readonly IValidator<CreateCoachTeachingLocationRequest> _createValidator;
        private readonly IValidator<UpdateCoachTeachingLocationRequest> _updateValidator;

        public CoachTeachingLocationService(
            ICoachTeachingLocationRepository locationRepository,
            ICoachRepository coachRepository,
            IValidator<CreateCoachTeachingLocationRequest> createValidator,
            IValidator<UpdateCoachTeachingLocationRequest> updateValidator)
        {
            _locationRepository = locationRepository;
            _coachRepository = coachRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<List<CoachTeachingLocationResponse>>> GetMyLocationsAsync(Guid coachId)
        {
            await EnsureCoachProfileExistsAsync(coachId);

            var locations = await _locationRepository.GetByCoachIdAsync(coachId);

            var response = locations
                .Select(x => x.ToResponse())
                .ToList();

            return Result<List<CoachTeachingLocationResponse>>.Success(response);
        }

        public async Task<Result<CoachTeachingLocationResponse>> CreateAsync(
            Guid coachId,
            CreateCoachTeachingLocationRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            EnsureValid(validationResult);

            await EnsureCoachProfileExistsAsync(coachId);

            var location = request.ToEntity(coachId);

            await _locationRepository.AddAsync(location);

            if (location.IsDefault)
            {
                await _locationRepository.ClearDefaultsAsync(coachId, location.Id);
            }

            return Result<CoachTeachingLocationResponse>.Success(location.ToResponse());
        }

        public async Task<Result<CoachTeachingLocationResponse>> UpdateAsync(
            Guid coachId,
            Guid id,
            UpdateCoachTeachingLocationRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            EnsureValid(validationResult);

            var location = await GetOwnedLocationAsync(coachId, id);

            location.ApplyUpdate(request);

            await _locationRepository.UpdateAsync(location);

            if (location.IsDefault)
            {
                await _locationRepository.ClearDefaultsAsync(coachId, location.Id);
            }

            return Result<CoachTeachingLocationResponse>.Success(location.ToResponse());
        }

        public async Task<Result> DeleteAsync(Guid coachId, Guid id)
        {
            var location = await GetOwnedLocationAsync(coachId, id);

            await _locationRepository.DeleteAsync(location);

            return Result.Success("Teaching location deleted successfully");
        }

        public async Task<Result<CoachTeachingLocationResponse>> SetDefaultAsync(
            Guid coachId,
            Guid id)
        {
            var location = await GetOwnedLocationAsync(coachId, id);

            location.IsDefault = true;
            location.UpdatedAt = DateTime.UtcNow;

            await _locationRepository.UpdateAsync(location);
            await _locationRepository.ClearDefaultsAsync(coachId, location.Id);

            return Result<CoachTeachingLocationResponse>.Success(location.ToResponse());
        }

        private async Task<Core.Entities.CoachTeachingLocation> GetOwnedLocationAsync(
            Guid coachId,
            Guid id)
        {
            var location = await _locationRepository.GetByIdForUpdateAsync(id);

            if (location == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachTeachingLocationNotFound,
                    "Coach teaching location not found");
            }

            if (location.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachTeachingLocationNotOwned,
                    "You can only manage your own teaching locations");
            }

            return location;
        }

        private async Task EnsureCoachProfileExistsAsync(Guid coachId)
        {
            var exists = await _coachRepository.ExistsByUserIdAsync(coachId);

            if (!exists)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachProfileNotFound,
                    "Coach profile not found");
            }
        }

        private static void EnsureValid(FluentValidation.Results.ValidationResult validationResult)
        {
            if (validationResult.IsValid)
            {
                return;
            }

            var details = validationResult.Errors
                .Select(x => x.ErrorMessage)
                .ToList();

            throw new ValidationException(
                ErrorCodes.ValidationError,
                "Invalid request data",
                details);
        }
    }
}
