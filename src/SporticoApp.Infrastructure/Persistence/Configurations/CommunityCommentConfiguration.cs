using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CommunityCommentConfiguration : IEntityTypeConfiguration<CommunityComment>
{
    public void Configure(EntityTypeBuilder<CommunityComment> builder)
    {
        builder.ToTable("community_comments", tb =>
            tb.HasCheckConstraint("chk_community_comments_reply_count_non_negative", "reply_count >= 0"));

        builder.HasKey(e => e.Id).HasName("community_comments_pkey");

        builder.HasIndex(e => new { e.PostId, e.Status }, "idx_community_comments_post_status");
        builder.HasIndex(e => e.ParentCommentId, "idx_community_comments_parent");
        builder.HasIndex(e => e.AuthorId, "idx_community_comments_author");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Content).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Status)
            .HasMaxLength(10)
            .HasDefaultValueSql("'active'::character varying")
            .IsRequired();
        builder.Property(e => e.ModerationReason).HasMaxLength(1000);
        builder.Property(e => e.ReplyCount).HasDefaultValue(0);
        builder.Property(e => e.ReactionCount).HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_community_comments_post");

        builder.HasOne(d => d.Author)
            .WithMany()
            .HasForeignKey(d => d.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_community_comments_author");

        builder.HasOne(d => d.ParentComment)
            .WithMany(p => p.Replies)
            .HasForeignKey(d => d.ParentCommentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_community_comments_parent");
    }
}
