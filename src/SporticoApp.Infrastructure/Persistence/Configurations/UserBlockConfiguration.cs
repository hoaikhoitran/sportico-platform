using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks", tb => tb.HasComment("One user blocking another (one-directional)"));

        builder.HasKey(e => new { e.BlockerId, e.BlockedUserId }).HasName("user_blocks_pkey");

        builder.HasIndex(e => e.BlockedUserId, "idx_user_blocks_blocked_user");

        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Blocker)
            .WithMany()
            .HasForeignKey(d => d.BlockerId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_blocks_blocker");

        builder.HasOne(d => d.BlockedUser)
            .WithMany()
            .HasForeignKey(d => d.BlockedUserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_blocks_blocked_user");
    }
}
