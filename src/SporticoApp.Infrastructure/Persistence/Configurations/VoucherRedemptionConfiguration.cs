using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class VoucherRedemptionConfiguration : IEntityTypeConfiguration<VoucherRedemption>
{
    public void Configure(EntityTypeBuilder<VoucherRedemption> builder)
    {
        builder.ToTable("voucher_redemptions", tb =>
            tb.HasComment("One learner's use of one voucher campaign against exactly one booking"));

        builder.HasKey(e => e.Id).HasName("voucher_redemptions_pkey");

        builder.HasIndex(e => e.BookingId, "uq_voucher_redemptions_booking").IsUnique();
        builder.HasIndex(e => new { e.VoucherCampaignId, e.Status }, "idx_voucher_redemptions_campaign_status");
        builder.HasIndex(e => new { e.LearnerId, e.VoucherCampaignId, e.Status }, "idx_voucher_redemptions_learner_campaign_status");
        builder.HasIndex(e => new { e.Status, e.ExpiresAt }, "idx_voucher_redemptions_status_expires_at");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ReleaseReason).HasMaxLength(50);
        builder.Property(e => e.OriginalAmount).HasPrecision(12, 2);
        builder.Property(e => e.DiscountAmount).HasPrecision(12, 2);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(0);

        builder.HasOne(d => d.VoucherCampaign)
            .WithMany(p => p.Redemptions)
            .HasForeignKey(d => d.VoucherCampaignId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_voucher_redemptions_campaign");

        builder.HasOne(d => d.Booking)
            .WithOne(p => p.VoucherRedemption)
            .HasForeignKey<VoucherRedemption>(d => d.BookingId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_voucher_redemptions_booking");

        builder.HasOne(d => d.Learner)
            .WithMany()
            .HasForeignKey(d => d.LearnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_voucher_redemptions_learner");
    }
}
