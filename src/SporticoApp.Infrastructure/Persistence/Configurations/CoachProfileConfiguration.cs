using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachProfileConfiguration : IEntityTypeConfiguration<CoachProfile>
{
    public void Configure(EntityTypeBuilder<CoachProfile> builder)
    {
        builder.ToTable("coach_profiles", tb => tb.HasComment("Hồ sơ huấn luyện viên"));

        builder.HasKey(e => e.UserId).HasName("coach_profiles_pkey");

        builder.HasIndex(e => new { e.Rating, e.TotalReviews }, "idx_coach_profiles_rating").IsDescending();

        builder.Property(e => e.UserId).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.ExperienceYears).HasDefaultValue(0);
        builder.Property(e => e.Headline).HasMaxLength(255);
        builder.Property(e => e.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(1000);
        builder.Property(e => e.TeachingAddress)
            .HasColumnName("teaching_address")
            .HasMaxLength(500);
        builder.Property(e => e.TeachingCity)
            .HasColumnName("teaching_city")
            .HasMaxLength(100);
        builder.Property(e => e.TeachingDistrict)
            .HasColumnName("teaching_district")
            .HasMaxLength(100);
        builder.Property(e => e.TeachingLatitude)
            .HasColumnName("teaching_latitude")
            .HasPrecision(9, 6);
        builder.Property(e => e.TeachingLongitude)
            .HasColumnName("teaching_longitude")
            .HasPrecision(9, 6);
        builder.Property(e => e.IsOnlineAvailable)
            .HasColumnName("is_online_available");
        builder.Property(e => e.IsOfflineAvailable)
            .HasColumnName("is_offline_available");
        builder.Property(e => e.Specialties)
            .HasColumnName("specialties")
            .HasMaxLength(1000);
        builder.Property(e => e.CertificationsSummary)
            .HasColumnName("certifications_summary")
            .HasMaxLength(2000);
        builder.Property(e => e.AchievementsSummary)
            .HasColumnName("achievements_summary")
            .HasMaxLength(2000);
        builder.Property(e => e.FacebookUrl)
            .HasColumnName("facebook_url")
            .HasMaxLength(1000);
        builder.Property(e => e.InstagramUrl)
            .HasColumnName("instagram_url")
            .HasMaxLength(1000);
        builder.Property(e => e.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(1000);
        builder.Property(e => e.Rating)
            .HasPrecision(3, 2)
            .HasDefaultValueSql("0.00")
            .HasComment("Cache: trung bình rating từ bảng reviews");
        builder.Property(e => e.TotalReviews)
            .HasDefaultValue(0)
            .HasComment("Cache: tổng số review");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.User)
            .WithOne(p => p.CoachProfile)
            .HasForeignKey<CoachProfile>(d => d.UserId)
            .HasConstraintName("fk_coach_profiles_user");
    }
}
