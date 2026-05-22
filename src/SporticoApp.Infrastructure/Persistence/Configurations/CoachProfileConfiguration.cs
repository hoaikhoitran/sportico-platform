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
