using SporticoApp.Application.DTOs.Withdrawals;
using SporticoApp.Application.Validators.Withdrawals;
using Xunit;

namespace SporticoApp.Application.Tests.Withdrawals;

/// <summary>
/// Part H: withdrawal amount must be a positive WHOLE VND value that fits the int sent to
/// PayOS — never silently truncated. (Balance &gt; AvailableBalance is enforced in the service.)
/// </summary>
public class CreateWithdrawalRequestValidatorTests
{
    private readonly CreateWithdrawalRequestValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100000)]
    public void Amount_ZeroOrNegative_Invalid(decimal amount)
    {
        var result = _validator.Validate(new CreateWithdrawalRequest { Amount = amount });
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(100000.5)]
    [InlineData(0.99)]
    [InlineData(283333.33)]
    public void Amount_Fractional_Invalid(decimal amount)
    {
        var result = _validator.Validate(new CreateWithdrawalRequest { Amount = amount });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("whole number"));
    }

    [Fact]
    public void Amount_AboveIntMax_Invalid()
    {
        var result = _validator.Validate(new CreateWithdrawalRequest { Amount = (decimal)int.MaxValue + 1 });
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("maximum"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100000)]
    [InlineData(2147483647)] // int.MaxValue
    public void Amount_ValidWhole_Valid(decimal amount)
    {
        var result = _validator.Validate(new CreateWithdrawalRequest { Amount = amount });
        Assert.True(result.IsValid);
    }
}
