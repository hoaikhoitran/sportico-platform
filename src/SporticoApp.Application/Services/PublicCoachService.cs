using Microsoft.Extensions.Logging;
using SporticoApp.Application.DTOs.PublicCoaches;
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
    public class PublicCoachService : IPublicCoachService
    {
        private readonly IPublicCoachRepository _publicCoachRepository;
        private readonly ILogger<PublicCoachService> _logger;
        public PublicCoachService(
            IPublicCoachRepository publicCoachRepository,
            ILogger<PublicCoachService> logger)
        {
            _publicCoachRepository = publicCoachRepository;
            _logger = logger;
        }
        public async Task<Result<PublicCoachDetailResponse>> GetByIdAsync(Guid coachId)
        {
            _logger.LogInformation(
                "Public coach detail requested. CoachId={CoachId}", coachId);

            var coach = await _publicCoachRepository.GetByIdAsync(coachId);
            if (coach == null)
            {
                _logger.LogInformation(
                    "Public coach detail not found (no active coach profile). CoachId={CoachId}",
                    coachId);

                throw new NotFoundException(
                    ErrorCodes.CoachProfileNotFound,
                    "Coach not found."
                    );
            }

            _logger.LogInformation(
                "Public coach detail found. CoachId={CoachId}, UserLoaded={UserLoaded}, " +
                "Sports={SportCount}, Media={MediaCount}, TrainingPackages={PackageCount}",
                coachId,
                coach.User != null,
                coach.CoachSports?.Count ?? 0,
                coach.Media?.Count ?? 0,
                coach.TrainingPackages?.Count ?? 0);

            return Result<PublicCoachDetailResponse>.Success(coach.ToPublicDetailResponse());
        }

        public async Task<Result<PagedResult<PublicCoachListItemResponse>>> GetPagedAsync(PublicCoachFilterRequest filter)
        { 
            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize <= 0 ? 10 : Math.Min(filter.PageSize, 50);

            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;

            var (items, totalCount) =
                await _publicCoachRepository.GetPagedAsync(filter);

            var response = new PagedResult<PublicCoachListItemResponse>(
                items.Select(x => x.ToPublicSummaryResponse()).ToList(),
                totalCount,
                pageNumber,
                pageSize);

            return Result<PagedResult<PublicCoachListItemResponse>>.Success(response);
        }
    }
}
