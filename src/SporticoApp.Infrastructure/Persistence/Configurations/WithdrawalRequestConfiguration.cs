using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.ToTable("withdrawal_requests", tb => tb.HasComment("Withdrawal requests from coach wallet"));

        builder.HasKey(e => e.Id).HasName("withdrawal_requests_pkey");

        builder.HasIndex(e => e.CoachId, "idx_withdrawal_requests_coach");
        builder.HasIndex(e => e.Status, "idx_withdrawal_requests_status");
        builder.HasIndex(e => e.CreatedAt, "idx_withdrawal_requests_created_at").IsDescending();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying")
            .HasComment("pending | approved | rejected | paid | cancelled");
        builder.Property(e => e.AdminNote).HasMaxLength(1000);

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.WithdrawalRequests)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_withdrawal_requests_coach");

        builder.HasOne(d => d.Wallet)
            .WithMany()
            .HasForeignKey(d => d.CoachWalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_withdrawal_requests_wallet");

        builder.HasOne(d => d.PayoutAccount)
            .WithMany()
            .HasForeignKey(d => d.CoachPayoutAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_withdrawal_requests_payout_account");
    }
}
