using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts", tb => tb.HasComment("Bài đăng dịch vụ huấn luyện"));

        builder.HasKey(e => e.Id).HasName("posts_pkey");

        builder.HasIndex(e => e.CoachId, "idx_posts_coach");
        builder.HasIndex(e => e.CreatedAt, "idx_posts_created_at")
            .IsDescending()
            .HasFilter("((status)::text = 'published'::text)");
        builder.HasIndex(e => e.SportId, "idx_posts_sport");
        builder.HasIndex(e => e.Status, "idx_posts_status")
            .HasFilter("((status)::text = 'published'::text)");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsOnline).HasDefaultValue(false);
        builder.Property(e => e.Location).HasMaxLength(255);
        builder.Property(e => e.Price).HasPrecision(12, 2);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'draft'::character varying")
            .HasComment("draft | pending | published | archived | rejected");
        builder.Property(e => e.Title).HasMaxLength(255);
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.Posts)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_posts_coach");

        builder.HasOne(d => d.Sport)
            .WithMany(p => p.Posts)
            .HasForeignKey(d => d.SportId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_posts_sport");
    }
}
