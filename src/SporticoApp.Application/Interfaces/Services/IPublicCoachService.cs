using SporticoApp.Application.DTOs.PublicCoaches;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IPublicCoachService
    {
        Task<Result<PagedResult<PublicCoachListItemResponse>>> GetPagedAsync(
            PublicCoachFilterRequest filter);

        Task<Result<PublicCoachDetailResponse>> GetByIdAsync(Guid coachId);
    }
}
