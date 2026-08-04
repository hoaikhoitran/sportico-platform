using SporticoApp.Application.DTOs.Vouchers;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Application.Tests.Vouchers;

/// <summary>
/// In-memory fakes for VoucherService tests. "ForUpdate" reads return the SAME tracked instance as
/// non-tracked reads (shared reference) — this exercises the business-rule LOGIC (quota/budget gates
/// counting reserved+applied, guarded status transitions) but does NOT simulate a real EF Core
/// optimistic-concurrency conflict between two separate DbContexts; that requires a real Postgres
/// integration test (see docs/community-api.md limitations).
/// </summary>
internal sealed class FakeVoucherCampaignRepository : IVoucherCampaignRepository
{
    public readonly Dictionary<Guid, VoucherCampaign> Campaigns = new();
    public int SaveCount;

    public Task<VoucherCampaign?> GetByCodeAsync(string code)
        => Task.FromResult(Campaigns.Values.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase)));

    public Task<VoucherCampaign?> GetByCodeForUpdateAsync(string code) => GetByCodeAsync(code);

    public Task<VoucherCampaign?> GetByIdAsync(Guid id)
        => Task.FromResult(Campaigns.TryGetValue(id, out var c) ? c : null);

    public Task<VoucherCampaign?> GetByIdForUpdateAsync(Guid id) => GetByIdAsync(id);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
        => Task.FromResult(Campaigns.Values.Any(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase) && c.Id != excludeId));

    public Task<bool> HasAnyRedemptionAsync(Guid campaignId) => Task.FromResult(false);

    public Task<(List<VoucherCampaign> Items, int TotalCount)> GetPagedAsync(VoucherCampaignFilterRequest filter)
        => Task.FromResult((Campaigns.Values.ToList(), Campaigns.Count));

    public Task AddWithoutSaveAsync(VoucherCampaign campaign)
    {
        Campaigns[campaign.Id] = campaign;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeVoucherRedemptionRepository : IVoucherRedemptionRepository
{
    public readonly List<VoucherRedemption> Redemptions = new();
    public int SaveCount;

    public Task<VoucherRedemption?> GetByBookingIdForUpdateAsync(Guid bookingId)
        => Task.FromResult(Redemptions.FirstOrDefault(r => r.BookingId == bookingId));

    public Task<int> CountByLearnerAndCampaignAsync(Guid learnerId, Guid campaignId, IReadOnlyCollection<string> statuses)
        => Task.FromResult(Redemptions.Count(r =>
            r.LearnerId == learnerId && r.VoucherCampaignId == campaignId && statuses.Contains(r.Status)));

    public Task<(List<VoucherRedemption> Items, int TotalCount)> GetPagedByCampaignAsync(
        Guid campaignId, VoucherRedemptionFilterRequest filter)
    {
        var items = Redemptions.Where(r => r.VoucherCampaignId == campaignId).ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task<List<VoucherRedemption>> GetExpiredReservedAsync(DateTime nowUtc, int batchSize)
        => Task.FromResult(Redemptions
            .Where(r => r.Status == SporticoApp.Shared.Constants.VoucherRedemptionStatuses.Reserved &&
                        r.ExpiresAt != null && r.ExpiresAt < nowUtc)
            .Take(batchSize)
            .ToList());

    public Task AddWithoutSaveAsync(VoucherRedemption redemption)
    {
        Redemptions.Add(redemption);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeVoucherTrainingPackageRepository : SporticoApp.Application.Interfaces.Repositories.ITrainingPackageRepository
{
    public TrainingPackage? Package;

    public FakeVoucherTrainingPackageRepository(TrainingPackage? package = null) => Package = package;

    public Task<TrainingPackage?> GetByIdAsync(Guid id) => Task.FromResult(Package != null && Package.Id == id ? Package : null);
    public Task<List<TrainingPackageSessionSlot>> GetSessionSlotsForUpdateAsync(Guid packageId) => Task.FromResult(new List<TrainingPackageSessionSlot>());
    public Task<TrainingPackage?> GetByIdWithCoachAsync(Guid id) => throw new NotImplementedException();
    public Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedWithCoachAsync(SporticoApp.Application.DTOs.TrainingPackages.TrainingPackageFilterRequest filter) => throw new NotImplementedException();
    public Task<TrainingPackage?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
    public Task<TrainingPackage?> GetOwnedByIdAsync(Guid coachId, Guid id) => throw new NotImplementedException();
    public Task<TrainingPackage?> GetOwnedByIdForUpdateAsync(Guid coachId, Guid id) => throw new NotImplementedException();
    public Task<(List<TrainingPackage> Items, int TotalCount)> GetPagedAsync(SporticoApp.Application.DTOs.TrainingPackages.TrainingPackageFilterRequest filter) => throw new NotImplementedException();
    public Task AddAsync(TrainingPackage trainingPackage) => throw new NotImplementedException();
    public Task AddWithoutSaveAsync(TrainingPackage trainingPackage) => throw new NotImplementedException();
    public Task SaveChangesAsync() => Task.CompletedTask;
}
