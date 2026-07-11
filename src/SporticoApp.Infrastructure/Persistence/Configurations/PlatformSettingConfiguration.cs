using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> builder)
    {
        builder.ToTable("platform_settings", tb =>
        {
            tb.HasComment("Singleton platform-wide settings (editable platform commission)");
            tb.HasCheckConstraint(
                "chk_platform_settings_commission_rate",
                "commission_rate >= 0 AND commission_rate <= 1");
        });

        builder.HasKey(e => e.Id).HasName("platform_settings_pkey");

        // Same precision as bookings.platform_fee_rate so the snapshot copies losslessly.
        builder.Property(e => e.CommissionRate)
            .HasPrecision(5, 4)
            .HasComment("Fractional commission rate for NEW bookings: 0.0000 (0%) .. 1.0000 (100%)");

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        // Optimistic concurrency token (same pattern as CoachWallet.Version).
        builder.Property(e => e.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        // Seed the single authoritative row with the 0% default. Fixed values are required by
        // HasData; the timestamps are the introduction date of the feature, not now().
        builder.HasData(new PlatformSetting
        {
            Id = PlatformSetting.SingletonId,
            CommissionRate = 0m,
            UpdatedByUserId = null,
            CreatedAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            Version = 0
        });
    }
}
