using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
{
    public void Configure(EntityTypeBuilder<PostImage> builder)
    {
        builder.ToTable("post_images", tb => tb.HasComment("Hình ảnh kèm bài đăng"));

        builder.HasKey(e => e.Id).HasName("post_images_pkey");

        builder.HasIndex(e => new { e.PostId, e.OrderIndex }, "idx_post_images_post");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.OrderIndex).HasDefaultValue(0);

        builder.HasOne(d => d.Post)
            .WithMany(p => p.PostImages)
            .HasForeignKey(d => d.PostId)
            .HasConstraintName("fk_post_images_post");
    }
}
