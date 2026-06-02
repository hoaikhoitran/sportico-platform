using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<Result<CoachDashboardResponse>> GetCoachDashboardAsync(
            Guid coachId,
            DashboardFilterRequest filter)
        {
            ValidateRange(filter);
            var data = await _dashboardRepository.GetCoachDashboardAsync(coachId, filter.FromDate, filter.ToDate);
            return Result<CoachDashboardResponse>.Success(data);
        }

        public async Task<Result<AdminDashboardResponse>> GetAdminDashboardAsync(
            DashboardFilterRequest filter)
        {
            ValidateRange(filter);
            var data = await _dashboardRepository.GetAdminDashboardAsync(filter.FromDate, filter.ToDate);
            return Result<AdminDashboardResponse>.Success(data);
        }

        private static void ValidateRange(DashboardFilterRequest filter)
        {
            if (filter.FromDate.HasValue && filter.ToDate.HasValue &&
                filter.FromDate.Value > filter.ToDate.Value)
            {
                throw new ValidationException(
                    ErrorCodes.ValidationError,
                    "fromDate must be on or before toDate");
            }
        }
    }
}
