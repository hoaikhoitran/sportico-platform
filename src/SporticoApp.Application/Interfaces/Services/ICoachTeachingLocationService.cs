using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachTeachingLocationService
    {
        Task<Result<List<CoachTeachingLocationResponse>>> GetMyLocationsAsync(Guid coachId);

        Task<Result<CoachTeachingLocationResponse>> CreateAsync(
            Guid coachId,
            CreateCoachTeachingLocationRequest request);

        Task<Result<CoachTeachingLocationResponse>> UpdateAsync(
            Guid coachId,
            Guid id,
            UpdateCoachTeachingLocationRequest request);

        Task<Result> DeleteAsync(Guid coachId, Guid id);

        Task<Result<CoachTeachingLocationResponse>> SetDefaultAsync(Guid coachId, Guid id);
    }
}
