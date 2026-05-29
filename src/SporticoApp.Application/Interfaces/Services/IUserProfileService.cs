using SporticoApp.Application.DTOs.Users;
using SporticoApp.Shared.Responses;
using System;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IUserProfileService
    {
        Task<Result<CurrentUserResponse>> GetMeAsync(Guid userId);

        Task<Result<CurrentUserResponse>> UpdateMeAsync(Guid userId, UpdateMeRequest request);
    }
}
