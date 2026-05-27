using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingPlanExerciseConfiguration : IEntityTypeConfiguration<TrainingPlanExercise>
{
    public void Configure(EntityTypeBuilder<TrainingPlanExercise> builder)
    {
        builder.ToTable("training_plan_exercises", tb => tb.HasComment("Exercises for training plan days"));

        builder.HasKey(e => e.Id).HasName("training_plan_exercises_pkey");

        builder.HasIndex(e => new { e.TrainingPlanDayId, e.OrderIndex }, "idx_training_plan_exercises_day_order");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.ExerciseName).HasMaxLength(200);
        builder.Property(e => e.Reps).HasMaxLength(50);
        builder.Property(e => e.Intensity).HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(d => d.TrainingPlanDay)
            .WithMany(p => p.Exercises)
            .HasForeignKey(d => d.TrainingPlanDayId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_training_plan_exercises_day");
    }
}
