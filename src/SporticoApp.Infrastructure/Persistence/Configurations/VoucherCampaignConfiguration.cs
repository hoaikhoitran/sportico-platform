using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class VoucherCampaignConfiguration : IEntityTypeConfiguration<VoucherCampaign>
{
    public void Configure(EntityTypeBuilder<VoucherCampaign> builder)
    {
        builder.ToTable("voucher_campaigns", tb =>
        {
            tb.HasComment("Admin-managed, platform-funded discount campaigns for TrainingPackage purchases");
            tb.HasCheckConstraint("chk_voucher_campaigns_reserved_count_non_negative", "reserved_count >= 0");
            tb.HasCheckConstraint("chk_voucher_campaigns_used_count_non_negative", "used_count >= 0");
            tb.HasCheckConstraint("chk_voucher_campaigns_reserved_discount_non_negative", "reserved_discount_amount >= 0");
            tb.HasCheckConstraint("chk_voucher_campaigns_used_discount_non_negative", "used_discount_amount >= 0");
        });

        builder.HasKey(e => e.Id).HasName("voucher_campaigns_pkey");

        // citext -> case-insensitive uniqueness for the redemption code.
        builder.Property(e => e.Code)
            .HasColumnType("citext")
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(e => e.Code, "uq_voucher_campaigns_code").IsUnique();

        builder.HasIndex(e => e.Status, "idx_voucher_campaigns_status");
        builder.HasIndex(e => e.StartAt, "idx_voucher_campaigns_start_at");
        builder.HasIndex(e => e.EndAt, "idx_voucher_campaigns_end_at");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.DiscountType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'draft'::character varying")
            .IsRequired();

        builder.Property(e => e.DiscountValue).HasPrecision(12, 2);
        builder.Property(e => e.MaxDiscountAmount).HasPrecision(12, 2);
        builder.Property(e => e.MinOrderAmount).HasPrecision(12, 2);
        builder.Property(e => e.BudgetAmount).HasPrecision(12, 2);
        builder.Property(e => e.ReservedDiscountAmount).HasPrecision(12, 2).HasDefaultValue(0m);
        builder.Property(e => e.UsedDiscountAmount).HasPrecision(12, 2).HasDefaultValue(0m);

        builder.Property(e => e.ReservedCount).HasDefaultValue(0);
        builder.Property(e => e.UsedCount).HasDefaultValue(0);

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.Property(e => e.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_voucher_campaigns_created_by");
    }
}
