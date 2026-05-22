using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachSportConfiguration : IEntityTypeConfiguration<CoachSport>
{
    public void Configure(EntityTypeBuilder<CoachSport> builder)
    {
        builder.ToTable("coach_sports", tb => tb.HasComment("Many-to-many: coach dạy những môn nào"));

        builder.HasKey(e => new { e.CoachId, e.SportId }).HasName("coach_sports_pkey");

        builder.HasIndex(e => e.SportId, "idx_coach_sports_sport");

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.CoachSports)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_coach_sports_coach");

        builder.HasOne(d => d.Sport)
            .WithMany(p => p.CoachSports)
            .HasForeignKey(d => d.SportId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_coach_sports_sport");
    }
}
