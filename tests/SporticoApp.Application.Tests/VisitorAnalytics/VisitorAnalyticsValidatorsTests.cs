using SporticoApp.Application.DTOs.VisitorAnalytics;
using SporticoApp.Application.Validators.VisitorAnalytics;
using Xunit;

namespace SporticoApp.Application.Tests.VisitorAnalytics;

/// <summary>Covers: date-range filter validation, invalid chart granularity, maximum date range.</summary>
public class VisitorAnalyticsValidatorsTests
{
    [Fact]
    public void VisitorAnalyticsFilter_FromAfterTo_Invalid()
    {
        var result = new VisitorAnalyticsFilterRequestValidator().Validate(new VisitorAnalyticsFilterRequest
        {
            FromDate = new DateTime(2026, 7, 21),
            ToDate = new DateTime(2026, 7, 1)
        });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("hour")]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    [InlineData("year")]
    public void VisitorsChartFilter_ValidGranularity_Valid(string granularity)
    {
        var result = new VisitorsChartFilterRequestValidator().Validate(new VisitorsChartFilterRequest
        {
            Granularity = granularity
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("minute")]
    [InlineData("century")]
    [InlineData("day'); DROP TABLE visitor_sessions;--")]
    public void VisitorsChartFilter_InvalidGranularity_Invalid(string granularity)
    {
        var result = new VisitorsChartFilterRequestValidator().Validate(new VisitorsChartFilterRequest
        {
            Granularity = granularity
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void VisitorsChartFilter_RangeExceedsLimit_Invalid()
    {
        var result = new VisitorsChartFilterRequestValidator().Validate(new VisitorsChartFilterRequest
        {
            FromDate = new DateTime(2018, 1, 1),
            ToDate = new DateTime(2026, 1, 1) // ~8 years
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TopPagesFilter_LimitOutOfRange_Invalid()
    {
        var result = new TopPagesFilterRequestValidator().Validate(new TopPagesFilterRequest
        {
            Limit = 0
        });

        Assert.False(result.IsValid);
    }
}
