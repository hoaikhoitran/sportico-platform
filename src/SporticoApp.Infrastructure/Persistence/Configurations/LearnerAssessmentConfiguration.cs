using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class LearnerAssessmentConfiguration : IEntityTypeConfiguration<LearnerAssessment>
{
    public void Configure(EntityTypeBuilder<LearnerAssessment> builder)
    {
        builder.ToTable("learner_assessments", tb => tb.HasComment("Learner assessment for personalization"));

        builder.HasKey(e => e.Id).HasName("learner_assessments_pkey");

        builder.HasIndex(e => e.BookingId, "uq_learner_assessments_booking").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.GoalType).HasMaxLength(50);
        builder.Property(e => e.GoalDescription).HasMaxLength(2000);
        builder.Property(e => e.HeightCm).HasPrecision(6, 2);
        builder.Property(e => e.WeightKg).HasPrecision(6, 2);
        builder.Property(e => e.BodyFatPercent).HasPrecision(6, 2);
        builder.Property(e => e.CurrentLevel).HasMaxLength(50);
        builder.Property(e => e.HealthNotes).HasMaxLength(3000);
        builder.Property(e => e.InjuryNotes).HasMaxLength(3000);
        builder.Property(e => e.TrainingHistory).HasMaxLength(3000);
        builder.Property(e => e.AvailableDaysPerWeek).HasMaxLength(100);
        builder.Property(e => e.EquipmentAvailable).HasMaxLength(500);

        builder.HasOne(d => d.Booking)
            .WithOne(p => p.LearnerAssessment)
            .HasForeignKey<LearnerAssessment>(d => d.BookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_learner_assessments_booking");

        builder.HasOne(d => d.Learner)
            .WithMany(p => p.LearnerAssessments)
            .HasForeignKey(d => d.LearnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_learner_assessments_learner");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.LearnerAssessments)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_learner_assessments_coach");
    }
}
