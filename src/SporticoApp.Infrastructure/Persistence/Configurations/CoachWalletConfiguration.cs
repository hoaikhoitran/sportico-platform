using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachWalletConfiguration : IEntityTypeConfiguration<CoachWallet>
{
    public void Configure(EntityTypeBuilder<CoachWallet> builder)
    {
        builder.ToTable("coach_wallets", tb => tb.HasComment("Internal wallet for coach"));

        builder.HasKey(e => e.Id).HasName("coach_wallets_pkey");

        builder.HasIndex(e => e.CoachId, "uq_coach_wallets_coach").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.AvailableBalance).HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(e => e.PendingBalance).HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(e => e.TotalEarned).HasPrecision(12, 2).HasDefaultValue(0);
        builder.Property(e => e.TotalWithdrawn).HasPrecision(12, 2).HasDefaultValue(0);

        builder.HasOne(d => d.Coach)
            .WithOne(p => p.Wallet)
            .HasForeignKey<CoachWallet>(d => d.CoachId)
            .HasConstraintName("fk_coach_wallets_coach");
    }
}
