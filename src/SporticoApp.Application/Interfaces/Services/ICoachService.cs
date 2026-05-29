using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachService
    {
        Task<Result<CoachProfileResponse>> RegisterCoachAsync(
            Guid userId,
            RegisterCoachRequest request);

        Task<Result<CoachProfileResponse>> GetMyProfileAsync(Guid coachId);

        Task<Result<CoachProfileResponse>> UpdateMyProfileAsync(
            Guid coachId,
            UpdateCoachProfileRequest request);
    }
}
