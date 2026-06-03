using SporticoApp.Application.DTOs.Users;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IUserPublicService
    {
        Task<Result<PublicUserResponse>> GetByIdAsync(Guid id);
    }
}
