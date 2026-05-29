using SporticoApp.Application.DTOs.Coaches;
using SporticoApp.Shared.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface ICoachProfileMediaService
    {
        Task<Result<List<CoachProfileMediaResponse>>> GetMyMediaAsync(Guid coachId);

        Task<Result<CoachProfileMediaResponse>> CreateAsync(
            Guid coachId,
            CreateCoachProfileMediaRequest request);

        Task<Result<CoachProfileMediaResponse>> UpdateAsync(
            Guid coachId,
            Guid id,
            UpdateCoachProfileMediaRequest request);

        Task<Result> DeleteAsync(Guid coachId, Guid id);
    }
}
