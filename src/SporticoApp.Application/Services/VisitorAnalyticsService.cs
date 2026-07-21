using FluentValidation;
using SporticoApp.Application.DTOs.VisitorAnalytics;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Responses;

namespace SporticoApp.Application.Services
{
    using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

    /// <summary>
    /// Orchestrates the admin visitor-analytics dashboard. Every method delegates aggregation to
    /// <see cref="IVisitorAnalyticsRepository"/> — this service owns only validation and composition.
    /// </summary>
    public class VisitorAnalyticsService : IVisitorAnalyticsService
    {
        private const int DashboardTopPagesLimit = 5;

        private readonly IVisitorAnalyticsRepository _repository;
        private readonly IValidator<VisitorAnalyticsFilterRequest> _filterValidator;
        private readonly IValidator<TopPagesFilterRequest> _topPagesValidator;
        private readonly IValidator<VisitorsChartFilterRequest> _chartValidator;

        public VisitorAnalyticsService(
            IVisitorAnalyticsRepository repository,
            IValidator<VisitorAnalyticsFilterRequest> filterValidator,
            IValidator<TopPagesFilterRequest> topPagesValidator,
            IValidator<VisitorsChartFilterRequest> chartValidator)
        {
            _repository = repository;
            _filterValidator = filterValidator;
            _topPagesValidator = topPagesValidator;
            _chartValidator = chartValidator;
        }

        public async Task<Result<VisitorDashboardResponse>> GetDashboardAsync(VisitorAnalyticsFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);

            var response = new VisitorDashboardResponse
            {
                VisitorStats = await _repository.GetVisitorStatsAsync(filter.FromDate, filter.ToDate),
                PageViewStats = await _repository.GetPageViewStatsAsync(filter.FromDate, filter.ToDate),
                VisitorsChart = await _repository.GetVisitorsChartAsync(filter.FromDate, filter.ToDate, "day"),
                TopPages = await _repository.GetTopPagesAsync(filter.FromDate, filter.ToDate, DashboardTopPagesLimit),
                Devices = await _repository.GetDeviceBreakdownAsync(filter.FromDate, filter.ToDate),
                Browsers = await _repository.GetBrowserBreakdownAsync(filter.FromDate, filter.ToDate),
                Countries = await _repository.GetCountryBreakdownAsync(filter.FromDate, filter.ToDate)
            };

            return Result<VisitorDashboardResponse>.Success(response);
        }

        public async Task<Result<VisitorStatsResponse>> GetVisitorStatsAsync(VisitorAnalyticsFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);
            var data = await _repository.GetVisitorStatsAsync(filter.FromDate, filter.ToDate);
            return Result<VisitorStatsResponse>.Success(data);
        }

        public async Task<Result<PageViewStatsResponse>> GetPageViewStatsAsync(VisitorAnalyticsFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);
            var data = await _repository.GetPageViewStatsAsync(filter.FromDate, filter.ToDate);
            return Result<PageViewStatsResponse>.Success(data);
        }

        public async Task<Result<List<TopPageItem>>> GetTopPagesAsync(TopPagesFilterRequest filter)
        {
            await ValidateAsync(_topPagesValidator, filter);
            var data = await _repository.GetTopPagesAsync(filter.FromDate, filter.ToDate, filter.Limit);
            return Result<List<TopPageItem>>.Success(data);
        }

        public async Task<Result<List<DeviceBreakdownItem>>> GetDeviceBreakdownAsync(VisitorAnalyticsFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);
            var data = await _repository.GetDeviceBreakdownAsync(filter.FromDate, filter.ToDate);
            return Result<List<DeviceBreakdownItem>>.Success(data);
        }

        public async Task<Result<List<BrowserBreakdownItem>>> GetBrowserBreakdownAsync(VisitorAnalyticsFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);
            var data = await _repository.GetBrowserBreakdownAsync(filter.FromDate, filter.ToDate);
            return Result<List<BrowserBreakdownItem>>.Success(data);
        }

        public async Task<Result<List<CountryBreakdownItem>>> GetCountryBreakdownAsync(VisitorAnalyticsFilterRequest filter)
        {
            await ValidateAsync(_filterValidator, filter);
            var data = await _repository.GetCountryBreakdownAsync(filter.FromDate, filter.ToDate);
            return Result<List<CountryBreakdownItem>>.Success(data);
        }

        public async Task<Result<List<VisitorsChartPoint>>> GetVisitorsChartAsync(VisitorsChartFilterRequest filter)
        {
            await ValidateAsync(_chartValidator, filter);
            var granularity = string.IsNullOrWhiteSpace(filter.Granularity) ? "day" : filter.Granularity;
            var data = await _repository.GetVisitorsChartAsync(filter.FromDate, filter.ToDate, granularity);
            return Result<List<VisitorsChartPoint>>.Success(data);
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
