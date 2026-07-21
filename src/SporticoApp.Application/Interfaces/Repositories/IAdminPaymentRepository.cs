using SporticoApp.Application.DTOs.AdminPayments;

namespace SporticoApp.Application.Interfaces.Repositories
{
    /// <summary>Read-only aggregate queries backing the admin payment dashboard.</summary>
    public interface IAdminPaymentRepository
    {
        Task<PaymentStatisticsResponse> GetStatisticsAsync(DateTime? fromDate, DateTime? toDate);

        Task<List<RevenueChartPoint>> GetRevenueChartAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string granularity);

        Task<List<PaymentMethodBreakdownItem>> GetPaymentMethodBreakdownAsync(
            DateTime? fromDate,
            DateTime? toDate);

        Task<List<TransactionStatusBreakdownItem>> GetTransactionStatusBreakdownAsync(
            DateTime? fromDate,
            DateTime? toDate);

        Task<List<TopCoachRevenueItem>> GetTopCoachesAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int limit);

        Task<List<TopSportRevenueItem>> GetTopSportsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int limit);

        Task<(List<AdminTransactionResponse> Items, int TotalCount)> GetTransactionsPagedAsync(
            AdminPaymentFilterRequest filter);
    }
}
