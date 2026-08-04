using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.ToTable("community_posts", tb =>
        {
            tb.HasComment("Community forum / player-recruitment posts (independent of the legacy post module)");
            tb.HasCheckConstraint("chk_community_posts_accepted_non_negative", "accepted_participants >= 0");
            tb.HasCheckConstraint("chk_community_posts_comment_count_non_negative", "comment_count >= 0");
            tb.HasCheckConstraint("chk_community_posts_reaction_count_non_negative", "reaction_count >= 0");
            tb.HasCheckConstraint("chk_community_posts_application_count_non_negative", "application_count >= 0");
            tb.HasCheckConstraint("chk_community_posts_view_count_non_negative", "view_count >= 0");
        });

        builder.HasKey(e => e.Id).HasName("community_posts_pkey");

        builder.HasIndex(e => e.Status, "idx_community_posts_status");
        builder.HasIndex(e => e.PostType, "idx_community_posts_post_type");
        builder.HasIndex(e => e.SportId, "idx_community_posts_sport");
        builder.HasIndex(e => e.AuthorId, "idx_community_posts_author");
        builder.HasIndex(e => e.CreatedAt, "idx_community_posts_created_at").IsDescending();
        builder.HasIndex(e => e.StartAt, "idx_community_posts_start_at");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.PostType).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).HasMaxLength(5000).IsRequired();
        builder.Property(e => e.LocationName).HasMaxLength(200);
        builder.Property(e => e.Address).HasMaxLength(300);
        builder.Property(e => e.Level).HasMaxLength(30);
        builder.Property(e => e.FeePerPerson).HasPrecision(12, 2);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'draft'::character varying")
            .IsRequired();
        builder.Property(e => e.ModerationReason).HasMaxLength(1000);

        builder.Property(e => e.AllowComments).HasDefaultValue(true);
        builder.Property(e => e.AcceptedParticipants).HasDefaultValue(0);
        builder.Property(e => e.CommentCount).HasDefaultValue(0);
        builder.Property(e => e.ReactionCount).HasDefaultValue(0);
        builder.Property(e => e.ApplicationCount).HasDefaultValue(0);
        builder.Property(e => e.ViewCount).HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(0);

        builder.HasOne(d => d.Author)
            .WithMany()
            .HasForeignKey(d => d.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_community_posts_author");

        builder.HasOne(d => d.Sport)
            .WithMany()
            .HasForeignKey(d => d.SportId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_community_posts_sport");
    }
}
