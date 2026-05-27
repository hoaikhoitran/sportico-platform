using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachPayoutAccountConfiguration : IEntityTypeConfiguration<CoachPayoutAccount>
{
    public void Configure(EntityTypeBuilder<CoachPayoutAccount> builder)
    {
        builder.ToTable("coach_payout_accounts", tb => tb.HasComment("Coach payout account"));

        builder.HasKey(e => e.Id).HasName("coach_payout_accounts_pkey");

        builder.HasIndex(e => e.CoachId, "uq_coach_payout_accounts_coach").IsUnique();
        builder.HasIndex(e => e.Status, "idx_coach_payout_accounts_status");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.PayoutMethod).HasMaxLength(50);
        builder.Property(e => e.BankName).HasMaxLength(255);
        builder.Property(e => e.BankAccountNumber).HasMaxLength(50);
        builder.Property(e => e.BankAccountHolder).HasMaxLength(255);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying")
            .HasComment("pending | verified | rejected");

        builder.HasOne(d => d.Coach)
            .WithOne(p => p.PayoutAccount)
            .HasForeignKey<CoachPayoutAccount>(d => d.CoachId)
            .HasConstraintName("fk_coach_payout_accounts_coach");
    }
}
