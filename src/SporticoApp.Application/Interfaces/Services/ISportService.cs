using System.Threading.Tasks;
using SporticoApp.Application.DTOs.Sports;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ISportService
    {
        Task<Result<SportResponse>> CreateAsync(CreateSportRequest request);
    }
}
