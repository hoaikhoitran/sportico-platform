using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("sports", tb => tb.HasComment("Danh mục môn thể thao"));

        builder.HasKey(e => e.Id).HasName("sports_pkey");

        builder.HasIndex(e => e.IsActive, "idx_sports_active")
            .HasFilter("(is_active = true)");
        builder.HasIndex(e => e.Name, "sports_name_key").IsUnique();
        builder.HasIndex(e => e.Slug, "sports_slug_key").IsUnique();

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.Name).HasMaxLength(100);
        builder.Property(e => e.Slug)
            .HasMaxLength(120)
            .HasComment("URL-friendly identifier, vd: cau-long, bong-da");
    }
}
