using System.Threading.Tasks;
using SporticoApp.Application.DTOs.Sports;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ISportService
    {
        Task<Result<SportResponse>> CreateAsync(CreateSportRequest request);
        Task<Result<PagedResult<SportResponse>>> GetPagedAsync(
            SportFilterRequest filter);

        Task<Result<SportResponse>> GetByIdAsync(int id);

        Task<Result<SportResponse>> UpdateStatusAsync(
            int id,
            UpdateSportStatusRequest request);
    }
}
