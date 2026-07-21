using SporticoApp.Application.DTOs.AdminPayments;
using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Interfaces.Services
{
    public interface IAdminPaymentService
    {
        Task<Result<AdminPaymentDashboardResponse>> GetDashboardAsync(DashboardFilterRequest filter);

        Task<Result<PaymentStatisticsResponse>> GetStatisticsAsync(DashboardFilterRequest filter);

        Task<Result<RevenueSummaryResponse>> GetRevenueAsync(DashboardFilterRequest filter);

        Task<Result<List<RevenueChartPoint>>> GetRevenueChartAsync(RevenueChartFilterRequest filter);

        Task<Result<List<TopCoachRevenueItem>>> GetTopCoachesAsync(TopEntitiesFilterRequest filter);

        Task<Result<List<TopSportRevenueItem>>> GetTopSportsAsync(TopEntitiesFilterRequest filter);

        Task<Result<List<AdminTransactionResponse>>> GetRecentTransactionsAsync(int limit);

        Task<Result<PagedResult<AdminTransactionResponse>>> GetTransactionsAsync(AdminPaymentFilterRequest filter);
    }
}
