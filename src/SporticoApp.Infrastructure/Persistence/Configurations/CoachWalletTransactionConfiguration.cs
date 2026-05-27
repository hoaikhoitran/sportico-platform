using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachWalletTransactionConfiguration : IEntityTypeConfiguration<CoachWalletTransaction>
{
    public void Configure(EntityTypeBuilder<CoachWalletTransaction> builder)
    {
        builder.ToTable("coach_wallet_transactions", tb => tb.HasComment("Coach wallet transactions"));

        builder.HasKey(e => e.Id).HasName("coach_wallet_transactions_pkey");

        builder.HasIndex(e => e.CoachWalletId, "idx_coach_wallet_transactions_wallet");
        builder.HasIndex(e => e.CoachId, "idx_coach_wallet_transactions_coach");
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_coach_wallet_transactions_reference");
        builder.HasIndex(e => e.CreatedAt, "idx_coach_wallet_transactions_created_at").IsDescending();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.Direction).HasMaxLength(20);
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.ReferenceType).HasMaxLength(50);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasOne(d => d.Wallet)
            .WithMany(p => p.Transactions)
            .HasForeignKey(d => d.CoachWalletId)
            .HasConstraintName("fk_coach_wallet_transactions_wallet");
    }
}
