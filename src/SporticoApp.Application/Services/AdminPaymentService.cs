using FluentValidation;
using SporticoApp.Application.DTOs.AdminPayments;
using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    /// <summary>
    /// Orchestrates the admin payment dashboard. Every method delegates the actual aggregation to
    /// <see cref="IAdminPaymentRepository"/> — this service owns only request validation and
    /// response composition, never a second copy of the revenue/statistics business rules.
    /// </summary>
    public class AdminPaymentService : IAdminPaymentService
    {
        private const int RecentTransactionsDefaultLimit = 10;
        private const int DashboardTopEntitiesLimit = 5;

        private readonly IAdminPaymentRepository _adminPaymentRepository;
        private readonly IValidator<DashboardFilterRequest> _dashboardFilterValidator;
        private readonly IValidator<RevenueChartFilterRequest> _revenueChartFilterValidator;
        private readonly IValidator<TopEntitiesFilterRequest> _topEntitiesFilterValidator;
        private readonly IValidator<AdminPaymentFilterRequest> _paymentFilterValidator;

        public AdminPaymentService(
            IAdminPaymentRepository adminPaymentRepository,
            IValidator<DashboardFilterRequest> dashboardFilterValidator,
            IValidator<RevenueChartFilterRequest> revenueChartFilterValidator,
            IValidator<TopEntitiesFilterRequest> topEntitiesFilterValidator,
            IValidator<AdminPaymentFilterRequest> paymentFilterValidator)
        {
            _adminPaymentRepository = adminPaymentRepository;
            _dashboardFilterValidator = dashboardFilterValidator;
            _revenueChartFilterValidator = revenueChartFilterValidator;
            _topEntitiesFilterValidator = topEntitiesFilterValidator;
            _paymentFilterValidator = paymentFilterValidator;
        }

        public async Task<Result<AdminPaymentDashboardResponse>> GetDashboardAsync(DashboardFilterRequest filter)
        {
            await ValidateAsync(_dashboardFilterValidator, filter);

            var response = new AdminPaymentDashboardResponse
            {
                Statistics = await _adminPaymentRepository.GetStatisticsAsync(filter.FromDate, filter.ToDate),
                RevenueChart = await _adminPaymentRepository.GetRevenueChartAsync(filter.FromDate, filter.ToDate, "day"),
                PaymentMethodBreakdown = await _adminPaymentRepository.GetPaymentMethodBreakdownAsync(filter.FromDate, filter.ToDate),
                TransactionStatusBreakdown = await _adminPaymentRepository.GetTransactionStatusBreakdownAsync(filter.FromDate, filter.ToDate),
                TopCoaches = await _adminPaymentRepository.GetTopCoachesAsync(filter.FromDate, filter.ToDate, DashboardTopEntitiesLimit),
                TopSports = await _adminPaymentRepository.GetTopSportsAsync(filter.FromDate, filter.ToDate, DashboardTopEntitiesLimit)
            };

            var (recent, _) = await _adminPaymentRepository.GetTransactionsPagedAsync(new AdminPaymentFilterRequest
            {
                PageNumber = 1,
                PageSize = RecentTransactionsDefaultLimit,
                SortBy = "newest"
            });
            response.RecentTransactions = recent;

            return Result<AdminPaymentDashboardResponse>.Success(response);
        }

        public async Task<Result<PaymentStatisticsResponse>> GetStatisticsAsync(DashboardFilterRequest filter)
        {
            await ValidateAsync(_dashboardFilterValidator, filter);

            var data = await _adminPaymentRepository.GetStatisticsAsync(filter.FromDate, filter.ToDate);
            return Result<PaymentStatisticsResponse>.Success(data);
        }

        public async Task<Result<RevenueSummaryResponse>> GetRevenueAsync(DashboardFilterRequest filter)
        {
            await ValidateAsync(_dashboardFilterValidator, filter);

            // Reuses the exact same statistics query as GetStatisticsAsync — just projects the
            // revenue-only fields, so the two endpoints can never report different numbers.
            var stats = await _adminPaymentRepository.GetStatisticsAsync(filter.FromDate, filter.ToDate);

            return Result<RevenueSummaryResponse>.Success(new RevenueSummaryResponse
            {
                TotalRevenue = stats.TotalRevenue,
                PlatformRevenue = stats.PlatformRevenue,
                CoachRevenue = stats.CoachRevenue,
                RevenueToday = stats.RevenueToday,
                RevenueThisWeek = stats.RevenueThisWeek,
                RevenueThisMonth = stats.RevenueThisMonth,
                RevenueThisYear = stats.RevenueThisYear
            });
        }

        public async Task<Result<List<RevenueChartPoint>>> GetRevenueChartAsync(RevenueChartFilterRequest filter)
        {
            await ValidateAsync(_revenueChartFilterValidator, filter);

            var granularity = string.IsNullOrWhiteSpace(filter.Granularity) ? "day" : filter.Granularity;
            var data = await _adminPaymentRepository.GetRevenueChartAsync(filter.FromDate, filter.ToDate, granularity);
            return Result<List<RevenueChartPoint>>.Success(data);
        }

        public async Task<Result<List<TopCoachRevenueItem>>> GetTopCoachesAsync(TopEntitiesFilterRequest filter)
        {
            await ValidateAsync(_topEntitiesFilterValidator, filter);

            var data = await _adminPaymentRepository.GetTopCoachesAsync(filter.FromDate, filter.ToDate, filter.Limit);
            return Result<List<TopCoachRevenueItem>>.Success(data);
        }

        public async Task<Result<List<TopSportRevenueItem>>> GetTopSportsAsync(TopEntitiesFilterRequest filter)
        {
            await ValidateAsync(_topEntitiesFilterValidator, filter);

            var data = await _adminPaymentRepository.GetTopSportsAsync(filter.FromDate, filter.ToDate, filter.Limit);
            return Result<List<TopSportRevenueItem>>.Success(data);
        }

        public async Task<Result<List<AdminTransactionResponse>>> GetRecentTransactionsAsync(int limit)
        {
            var boundedLimit = Math.Clamp(limit <= 0 ? RecentTransactionsDefaultLimit : limit, 1, 50);

            // Reuses the same paginated-transactions query as the full /transactions endpoint.
            var (items, _) = await _adminPaymentRepository.GetTransactionsPagedAsync(new AdminPaymentFilterRequest
            {
                PageNumber = 1,
                PageSize = boundedLimit,
                SortBy = "newest"
            });

            return Result<List<AdminTransactionResponse>>.Success(items);
        }

        public async Task<Result<PagedResult<AdminTransactionResponse>>> GetTransactionsAsync(AdminPaymentFilterRequest filter)
        {
            await ValidateAsync(_paymentFilterValidator, filter);

            var (items, totalCount) = await _adminPaymentRepository.GetTransactionsPagedAsync(filter);

            var response = new PagedResult<AdminTransactionResponse>(
                items, totalCount, filter.PageNumber, filter.PageSize);

            return Result<PagedResult<AdminTransactionResponse>>.Success(response);
        }

        private static async Task ValidateAsync<T>(IValidator<T> validator, T request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var details = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                throw new ValidationException(ErrorCodes.ValidationError, "Invalid request data", details);
            }
        }
    }
}
