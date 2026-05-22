using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("follows", tb => tb.HasComment("User theo dõi user khác (chủ yếu learner follow coach)"));

        builder.HasKey(e => new { e.FollowerId, e.FollowingId }).HasName("follows_pkey");

        builder.HasIndex(e => e.FollowingId, "idx_follows_following");

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.follower)
            .WithMany(p => p.FollowsAsFollower)
            .HasForeignKey(d => d.FollowerId)
            .HasConstraintName("fk_follows_follower");

        builder.HasOne(d => d.following)
            .WithMany(p => p.FollowsAsFollowing)
            .HasForeignKey(d => d.FollowingId)
            .HasConstraintName("fk_follows_following");
    }
}
