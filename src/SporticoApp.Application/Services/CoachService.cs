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
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class CoachService : ICoachService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly ICoachRepository _coachRepository;
        private readonly ISportRepository _sportRepository;
        private readonly IValidator<RegisterCoachRequest> _validator;

        public CoachService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            ICoachRepository coachRepository,
            ISportRepository sportRepository,
            IValidator<RegisterCoachRequest> validator)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _coachRepository = coachRepository;
            _sportRepository = sportRepository;
            _validator = validator;
        }

        public async Task<Result<CoachProfileResponse>> RegisterCoachAsync(
            Guid userId,
            RegisterCoachRequest request)
        {
            var validationResult =
                await _validator.ValidateAsync(request);

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

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(
                    ErrorCodes.UserNotFound,
                    "User not found");
            }

            if (!string.Equals(
                    user.Status,
                    "active",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedException(
                    ErrorCodes.AccountNotActive,
                    "Account is not active");
            }

            var alreadyCoach =
                await _coachRepository.ExistsByUserIdAsync(userId);

            if (alreadyCoach)
            {
                throw new ConflictException(
                    ErrorCodes.CoachProfileAlreadyExists,
                    "Coach profile already exists");
            }

            var coachRole =
                await _roleRepository.GetByNameAsync(RoleConstants.Coach);

            if (coachRole == null)
            {
                throw new NotFoundException(
                    ErrorCodes.RoleNotFound,
                    "Coach role not found");
            }

            var distinctSportIds = request.SportIds
                .Distinct()
                .ToList();

            var activeSportIds =
                await _sportRepository.GetActiveSportIdsAsync(distinctSportIds);

            if (activeSportIds.Count != distinctSportIds.Count)
            {
                throw new ValidationException(
                    ErrorCodes.InvalidSport,
                    "One or more sport ids are invalid");
            }

            var coachProfile =
                request.ToEntity(userId);

            await _coachRepository.CreateCoachProfileAsync(
                coachProfile,
                coachRole.Id,
                activeSportIds);

            var response =
                coachProfile.ToResponse();

            return Result<CoachProfileResponse>.Success(response);
        }
    }
}
