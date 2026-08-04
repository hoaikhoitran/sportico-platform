using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostReactionConfiguration : IEntityTypeConfiguration<CommunityPostReaction>
{
    public void Configure(EntityTypeBuilder<CommunityPostReaction> builder)
    {
        builder.ToTable("community_post_reactions", tb => tb.HasComment("Like on a community post; MVP supports only 'like'"));

        builder.HasKey(e => new { e.PostId, e.UserId }).HasName("community_post_reactions_pkey");

        builder.HasIndex(e => e.UserId, "idx_community_post_reactions_user");

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Post)
            .WithMany(p => p.Reactions)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_community_post_reactions_post");

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_community_post_reactions_user");
    }
}
