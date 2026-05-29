using FluentValidation;
using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class UserProfileService : IUserProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IValidator<UpdateMeRequest> _updateValidator;

        public UserProfileService(
            IUserRepository userRepository,
            IValidator<UpdateMeRequest> updateValidator)
        {
            _userRepository = userRepository;
            _updateValidator = updateValidator;
        }

        public async Task<Result<CurrentUserResponse>> GetMeAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdWithProfilesAndRolesAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(
                    ErrorCodes.UserNotFound,
                    "User not found");
            }

            return Result<CurrentUserResponse>.Success(user.ToCurrentUserResponse());
        }

        public async Task<Result<CurrentUserResponse>> UpdateMeAsync(
            Guid userId,
            UpdateMeRequest request)
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

            var user = await _userRepository.GetByIdForUpdateAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(
                    ErrorCodes.UserNotFound,
                    "User not found");
            }

            user.ApplyUpdate(request);

            await _userRepository.SaveChangesAsync();

            // Reload with roles and profiles so the response is complete.
            var updated = await _userRepository.GetByIdWithProfilesAndRolesAsync(userId);

            return Result<CurrentUserResponse>.Success(
                (updated ?? user).ToCurrentUserResponse());
        }
    }
}
