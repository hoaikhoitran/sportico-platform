using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachProfileMediaConfiguration : IEntityTypeConfiguration<CoachProfileMedia>
{
    public void Configure(EntityTypeBuilder<CoachProfileMedia> builder)
    {
        builder.ToTable("coach_profile_media", tb => tb.HasComment("Media (image URLs) cho hồ sơ huấn luyện viên: certificate/award/gallery"));

        builder.HasKey(e => e.Id).HasName("coach_profile_media_pkey");

        builder.HasIndex(e => e.CoachId, "idx_coach_profile_media_coach");
        builder.HasIndex(e => e.MediaType, "idx_coach_profile_media_type");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CoachId).HasColumnName("coach_id");
        builder.Property(e => e.MediaType)
            .HasColumnName("media_type")
            .HasMaxLength(50);
        builder.Property(e => e.MediaUrl)
            .HasColumnName("media_url")
            .HasMaxLength(1000);
        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(200);
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);
        builder.Property(e => e.OrderIndex)
            .HasColumnName("order_index")
            .HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.Media)
            .HasForeignKey(d => d.CoachId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_coach_profile_media_coach");
    }
}
