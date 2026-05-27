using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingPlanDayConfiguration : IEntityTypeConfiguration<TrainingPlanDay>
{
    public void Configure(EntityTypeBuilder<TrainingPlanDay> builder)
    {
        builder.ToTable("training_plan_days", tb => tb.HasComment("Daily breakdown for training plans"));

        builder.HasKey(e => e.Id).HasName("training_plan_days_pkey");

        builder.HasIndex(e => new { e.TrainingPlanWeekId, e.DayNumber }, "idx_training_plan_days_week_day").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasOne(d => d.TrainingPlanWeek)
            .WithMany(p => p.Days)
            .HasForeignKey(d => d.TrainingPlanWeekId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_training_plan_days_week");
    }
}
