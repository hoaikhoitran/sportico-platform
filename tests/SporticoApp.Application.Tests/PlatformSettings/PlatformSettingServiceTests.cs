using SporticoApp.Application.DTOs.PlatformSettings;
using SporticoApp.Application.Services;
using SporticoApp.Application.Tests.Payments; // FakePlatformSettingRepository
using SporticoApp.Application.Validators.PlatformSettings;
using Xunit;
using ValidationException = SporticoApp.Shared.Exceptions.ValidationException;

namespace SporticoApp.Application.Tests.PlatformSettings;

/// <summary>
/// Admin platform-commission settings: percent ⇄ fractional-rate conversion at the Application
/// boundary, validation limits (0..100, two decimal places), and audit fields on update.
/// Uses the REAL validator so the rejection rules are covered end to end.
/// </summary>
public class PlatformSettingServiceTests
{
    private static readonly Guid AdminId = Guid.Parse("ad000000-0000-0000-0000-000000000001");

    private static (PlatformSettingService Svc, FakePlatformSettingRepository Repo) Build(decimal rate = 0m)
    {
        var repo = new FakePlatformSettingRepository(rate);
        var svc = new PlatformSettingService(repo, new UpdatePlatformCommissionRequestValidator());
        return (svc, repo);
    }

    [Fact]
    public async Task Get_ReturnsPersistedRateAsPercent()
    {
        var (svc, _) = Build(rate: 0.125m);

        var result = await svc.GetCommissionAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(12.5m, result.Data!.CommissionPercent);
    }

    [Fact]
    public async Task Get_DefaultSeededSetting_ReturnsZeroPercent()
    {
        var (svc, _) = Build();

        var result = await svc.GetCommissionAsync();

        Assert.Equal(0m, result.Data!.CommissionPercent);
    }

    [Fact]
    public async Task Update_ToZeroPercent_PersistsZeroRate()
    {
        var (svc, repo) = Build(rate: 0.15m);

        var result = await svc.UpdateCommissionAsync(
            AdminId, new UpdatePlatformCommissionRequest { CommissionPercent = 0m });

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Data!.CommissionPercent);
        Assert.Equal(0m, repo.Setting.CommissionRate);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task Update_ToFractionalPercent_PersistsFractionalRate_AndAuditFields()
    {
        var (svc, repo) = Build();
        var before = repo.Setting.UpdatedAt;
        var versionBefore = repo.Setting.Version;

        var result = await svc.UpdateCommissionAsync(
            AdminId, new UpdatePlatformCommissionRequest { CommissionPercent = 12.5m });

        Assert.True(result.IsSuccess);
        Assert.Equal(12.5m, result.Data!.CommissionPercent);
        Assert.Equal(0.125m, repo.Setting.CommissionRate);   // stored as a fractional rate
        Assert.Equal(AdminId, repo.Setting.UpdatedByUserId); // audit: who
        Assert.True(repo.Setting.UpdatedAt >= before);       // audit: when
        Assert.Equal(versionBefore + 1, repo.Setting.Version);
        Assert.Equal(AdminId, result.Data.UpdatedByUserId);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task Update_HundredPercent_IsAccepted()
    {
        var (svc, repo) = Build();

        var result = await svc.UpdateCommissionAsync(
            AdminId, new UpdatePlatformCommissionRequest { CommissionPercent = 100m });

        Assert.Equal(100m, result.Data!.CommissionPercent);
        Assert.Equal(1m, repo.Setting.CommissionRate);
    }

    [Theory]
    [InlineData("-0.01")]   // negative
    [InlineData("-5")]
    [InlineData("100.01")]  // above 100
    [InlineData("150")]
    [InlineData("12.345")]  // more than two decimal places
    public async Task Update_InvalidPercent_ThrowsValidation_AndDoesNotSave(string percentRaw)
    {
        var percent = decimal.Parse(percentRaw, System.Globalization.CultureInfo.InvariantCulture);
        var (svc, repo) = Build();

        await Assert.ThrowsAsync<ValidationException>(() =>
            svc.UpdateCommissionAsync(AdminId, new UpdatePlatformCommissionRequest { CommissionPercent = percent }));

        Assert.Equal(0, repo.SaveCount);
        Assert.Equal(0m, repo.Setting.CommissionRate); // unchanged
    }

    [Fact]
    public async Task Update_MissingPercent_ThrowsValidation()
    {
        var (svc, repo) = Build();

        await Assert.ThrowsAsync<ValidationException>(() =>
            svc.UpdateCommissionAsync(AdminId, new UpdatePlatformCommissionRequest { CommissionPercent = null }));

        Assert.Equal(0, repo.SaveCount);
    }
}
