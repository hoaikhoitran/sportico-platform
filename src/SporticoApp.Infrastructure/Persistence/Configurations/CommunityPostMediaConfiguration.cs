using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostMediaConfiguration : IEntityTypeConfiguration<CommunityPostMedia>
{
    public void Configure(EntityTypeBuilder<CommunityPostMedia> builder)
    {
        builder.ToTable("community_post_media", tb =>
            tb.HasCheckConstraint("chk_community_post_media_order_index_non_negative", "order_index >= 0"));

        builder.HasKey(e => e.Id).HasName("community_post_media_pkey");

        builder.HasIndex(e => new { e.PostId, e.OrderIndex }, "idx_community_post_media_post");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.MediaType).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Url).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.StorageKey).HasMaxLength(500);
        builder.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
        builder.Property(e => e.MimeType).HasMaxLength(100);
        builder.Property(e => e.Status)
            .HasMaxLength(10)
            .HasDefaultValueSql("'active'::character varying")
            .IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Post)
            .WithMany(p => p.Media)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_community_post_media_post");
    }
}
