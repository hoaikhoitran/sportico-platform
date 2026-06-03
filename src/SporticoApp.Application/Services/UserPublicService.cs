using SporticoApp.Application.DTOs.Users;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Application.Mappings;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    public class UserPublicService : IUserPublicService
    {
        private readonly IUserRepository _userRepository;

        public UserPublicService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<PublicUserResponse>> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdWithProfilesAndRolesAsync(id);
            if (user == null)
            {
                throw new NotFoundException(ErrorCodes.UserNotFound, "User not found");
            }

            return Result<PublicUserResponse>.Success(user.ToPublicUserResponse());
        }
    }
}
