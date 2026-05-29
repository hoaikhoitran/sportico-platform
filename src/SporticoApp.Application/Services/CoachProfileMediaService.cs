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

    public class CoachProfileMediaService : ICoachProfileMediaService
    {
        private readonly ICoachProfileMediaRepository _mediaRepository;
        private readonly ICoachRepository _coachRepository;
        private readonly IValidator<CreateCoachProfileMediaRequest> _createValidator;
        private readonly IValidator<UpdateCoachProfileMediaRequest> _updateValidator;

        public CoachProfileMediaService(
            ICoachProfileMediaRepository mediaRepository,
            ICoachRepository coachRepository,
            IValidator<CreateCoachProfileMediaRequest> createValidator,
            IValidator<UpdateCoachProfileMediaRequest> updateValidator)
        {
            _mediaRepository = mediaRepository;
            _coachRepository = coachRepository;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<Result<List<CoachProfileMediaResponse>>> GetMyMediaAsync(Guid coachId)
        {
            await EnsureCoachProfileExistsAsync(coachId);

            var media = await _mediaRepository.GetByCoachIdAsync(coachId);

            var response = media
                .Select(x => x.ToResponse())
                .ToList();

            return Result<List<CoachProfileMediaResponse>>.Success(response);
        }

        public async Task<Result<CoachProfileMediaResponse>> CreateAsync(
            Guid coachId,
            CreateCoachProfileMediaRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            EnsureValid(validationResult);

            await EnsureCoachProfileExistsAsync(coachId);

            var media = request.ToEntity(coachId);

            await _mediaRepository.AddAsync(media);

            return Result<CoachProfileMediaResponse>.Success(media.ToResponse());
        }

        public async Task<Result<CoachProfileMediaResponse>> UpdateAsync(
            Guid coachId,
            Guid id,
            UpdateCoachProfileMediaRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            EnsureValid(validationResult);

            var media = await GetOwnedMediaAsync(coachId, id);

            media.ApplyUpdate(request);

            await _mediaRepository.UpdateAsync(media);

            return Result<CoachProfileMediaResponse>.Success(media.ToResponse());
        }

        public async Task<Result> DeleteAsync(Guid coachId, Guid id)
        {
            var media = await GetOwnedMediaAsync(coachId, id);

            await _mediaRepository.DeleteAsync(media);

            return Result.Success("Media deleted successfully");
        }

        private async Task<Core.Entities.CoachProfileMedia> GetOwnedMediaAsync(
            Guid coachId,
            Guid id)
        {
            var media = await _mediaRepository.GetByIdForUpdateAsync(id);

            if (media == null)
            {
                throw new NotFoundException(
                    ErrorCodes.CoachProfileMediaNotFound,
                    "Coach profile media not found");
            }

            if (media.CoachId != coachId)
            {
                throw new ForbiddenException(
                    ErrorCodes.CoachProfileMediaNotOwned,
                    "You can only manage your own media");
            }

            return media;
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
