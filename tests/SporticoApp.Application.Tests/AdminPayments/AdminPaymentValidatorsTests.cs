using SporticoApp.Application.DTOs.AdminPayments;
using SporticoApp.Application.DTOs.Dashboard;
using SporticoApp.Application.Validators.AdminPayments;
using SporticoApp.Application.Validators.Dashboard;
using Xunit;

namespace SporticoApp.Application.Tests.AdminPayments;

/// <summary>Covers: date-range filter validation, invalid chart granularity, maximum date range.</summary>
public class AdminPaymentValidatorsTests
{
    // ── date filters ────────────────────────────────────────────────────────

    [Fact]
    public void DashboardFilter_FromAfterTo_Invalid()
    {
        var result = new DashboardFilterRequestValidator().Validate(new DashboardFilterRequest
        {
            FromDate = new DateTime(2026, 7, 21),
            ToDate = new DateTime(2026, 7, 1)
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void DashboardFilter_FromBeforeTo_Valid()
    {
        var result = new DashboardFilterRequestValidator().Validate(new DashboardFilterRequest
        {
            FromDate = new DateTime(2026, 7, 1),
            ToDate = new DateTime(2026, 7, 21)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DashboardFilter_NoDates_Valid()
    {
        var result = new DashboardFilterRequestValidator().Validate(new DashboardFilterRequest());

        Assert.True(result.IsValid);
    }

    // ── invalid granularity ─────────────────────────────────────────────────

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    [InlineData("year")]
    public void RevenueChartFilter_ValidGranularity_Valid(string granularity)
    {
        var result = new RevenueChartFilterRequestValidator().Validate(new RevenueChartFilterRequest
        {
            Granularity = granularity
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("hour")] // not supported for payments (only visitor chart supports hour)
    [InlineData("decade")]
    [InlineData("'; DROP TABLE bookings; --")]
    public void RevenueChartFilter_InvalidGranularity_Invalid(string granularity)
    {
        var result = new RevenueChartFilterRequestValidator().Validate(new RevenueChartFilterRequest
        {
            Granularity = granularity
        });

        Assert.False(result.IsValid);
    }

    // ── maximum date range ──────────────────────────────────────────────────

    [Fact]
    public void RevenueChartFilter_RangeWithinLimit_Valid()
    {
        var result = new RevenueChartFilterRequestValidator().Validate(new RevenueChartFilterRequest
        {
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 12, 31) // ~1 year, under the 730-day cap
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RevenueChartFilter_RangeExceedsLimit_Invalid()
    {
        var result = new RevenueChartFilterRequestValidator().Validate(new RevenueChartFilterRequest
        {
            FromDate = new DateTime(2020, 1, 1),
            ToDate = new DateTime(2026, 1, 1) // ~6 years, exceeds the 730-day cap
        });

        Assert.False(result.IsValid);
    }

    // ── AdminPaymentFilterRequest: status/method/sort whitelists ───────────

    [Theory]
    [InlineData("pending")]
    [InlineData("paid")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public void PaymentFilter_ValidStatus_Valid(string status)
    {
        var result = new AdminPaymentFilterRequestValidator().Validate(new AdminPaymentFilterRequest
        {
            Status = status
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PaymentFilter_InvalidStatus_Invalid()
    {
        var result = new AdminPaymentFilterRequestValidator().Validate(new AdminPaymentFilterRequest
        {
            Status = "refunded" // not a real Payment.Status value
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void PaymentFilter_InvalidMethod_Invalid()
    {
        var result = new AdminPaymentFilterRequestValidator().Validate(new AdminPaymentFilterRequest
        {
            Method = "vnpay" // does not exist in this codebase
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void PaymentFilter_InvalidPageSize_Invalid()
    {
        var result = new AdminPaymentFilterRequestValidator().Validate(new AdminPaymentFilterRequest
        {
            PageSize = 1000
        });

        Assert.False(result.IsValid);
    }
}
