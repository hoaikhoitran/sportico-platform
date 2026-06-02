using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<Result<CoachDashboardResponse>> GetCoachDashboardAsync(
            Guid coachId,
            DashboardFilterRequest filter);

        Task<Result<AdminDashboardResponse>> GetAdminDashboardAsync(
            DashboardFilterRequest filter);
    }
}
