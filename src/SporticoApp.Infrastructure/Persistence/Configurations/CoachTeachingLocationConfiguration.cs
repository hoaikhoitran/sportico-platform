using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachTeachingLocationConfiguration : IEntityTypeConfiguration<CoachTeachingLocation>
{
    public void Configure(EntityTypeBuilder<CoachTeachingLocation> builder)
    {
        builder.ToTable("coach_teaching_locations", tb => tb.HasComment("Các địa điểm dạy offline của huấn luyện viên"));

        builder.HasKey(e => e.Id).HasName("coach_teaching_locations_pkey");

        builder.HasIndex(e => e.CoachId, "idx_coach_teaching_locations_coach");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CoachId).HasColumnName("coach_id");
        builder.Property(e => e.Address)
            .HasColumnName("address")
            .HasMaxLength(500);
        builder.Property(e => e.City)
            .HasColumnName("city")
            .HasMaxLength(100);
        builder.Property(e => e.District)
            .HasColumnName("district")
            .HasMaxLength(100);
        builder.Property(e => e.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(9, 6);
        builder.Property(e => e.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(9, 6);
        builder.Property(e => e.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.TeachingLocations)
            .HasForeignKey(d => d.CoachId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_coach_teaching_locations_coach");
    }
}
