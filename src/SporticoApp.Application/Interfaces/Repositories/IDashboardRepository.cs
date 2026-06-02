using SporticoApp.Application.DTOs.Dashboard;

namespace SporticoApp.Application.Interfaces.Repositories
{
    public interface IDashboardRepository
    {
        Task<CoachDashboardResponse> GetCoachDashboardAsync(
            Guid coachId,
            DateTime? fromDate,
            DateTime? toDate);

        Task<AdminDashboardResponse> GetAdminDashboardAsync(
            DateTime? fromDate,
            DateTime? toDate);
    }
}
