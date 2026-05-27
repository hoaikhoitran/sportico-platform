using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingPlanWeekConfiguration : IEntityTypeConfiguration<TrainingPlanWeek>
{
    public void Configure(EntityTypeBuilder<TrainingPlanWeek> builder)
    {
        builder.ToTable("training_plan_weeks", tb => tb.HasComment("Weekly breakdown for training plans"));

        builder.HasKey(e => e.Id).HasName("training_plan_weeks_pkey");

        builder.HasIndex(e => new { e.TrainingPlanId, e.WeekNumber }, "idx_training_plan_weeks_plan_week").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Focus).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasOne(d => d.TrainingPlan)
            .WithMany(p => p.Weeks)
            .HasForeignKey(d => d.TrainingPlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_training_plan_weeks_plan");
    }
}
