using SporticoApp.Shared.Payments;
using Xunit;

namespace SporticoApp.Application.Tests.Payments;

/// <summary>Centralized PayOS payout status normalization — case-insensitive, variant-tolerant.</summary>
public class PayoutStatusTests
{
    [Theory]
    [InlineData("SUCCESS")]
    [InlineData("success")]
    [InlineData("Succeeded")]
    [InlineData("PAID")]
    [InlineData("completed")]
    [InlineData("  DONE  ")]
    public void Success_States_Classify_As_Success(string raw)
    {
        Assert.True(PayoutStatus.IsSuccess(raw));
        Assert.False(PayoutStatus.IsFailure(raw));
        Assert.False(PayoutStatus.IsProcessing(raw));
        Assert.Equal(PayoutOutcome.Success, PayoutStatus.Classify(raw));
    }

    [Theory]
    [InlineData("FAILED")]
    [InlineData("failed")]
    [InlineData("CANCELLED")]
    [InlineData("canceled")]
    [InlineData("REJECTED")]
    [InlineData("error")]
    public void Failure_States_Classify_As_Failed(string raw)
    {
        Assert.True(PayoutStatus.IsFailure(raw));
        Assert.False(PayoutStatus.IsSuccess(raw));
        Assert.Equal(PayoutOutcome.Failed, PayoutStatus.Classify(raw));
    }

    [Theory]
    [InlineData("PROCESSING")]
    [InlineData("processing")]
    [InlineData("PENDING")]
    [InlineData("RECEIVED")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("something_unknown")]
    public void Processing_Or_Unknown_States_Classify_As_Processing(string? raw)
    {
        Assert.True(PayoutStatus.IsProcessing(raw));
        Assert.False(PayoutStatus.IsSuccess(raw));
        Assert.False(PayoutStatus.IsFailure(raw));
        Assert.Equal(PayoutOutcome.Processing, PayoutStatus.Classify(raw));
    }

    [Fact]
    public void Normalize_TrimsAndUppercases()
    {
        Assert.Equal("SUCCESS", PayoutStatus.Normalize("  success "));
        Assert.Equal(string.Empty, PayoutStatus.Normalize(null));
    }
}
