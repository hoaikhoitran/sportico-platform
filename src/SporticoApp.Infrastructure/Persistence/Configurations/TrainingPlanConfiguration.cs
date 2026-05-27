using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingPlanConfiguration : IEntityTypeConfiguration<TrainingPlan>
{
    public void Configure(EntityTypeBuilder<TrainingPlan> builder)
    {
        builder.ToTable("training_plans", tb => tb.HasComment("Training plans for bookings"));

        builder.HasKey(e => e.Id).HasName("training_plans_pkey");

        builder.HasIndex(e => e.BookingId, "uq_training_plans_booking").IsUnique();
        builder.HasIndex(e => e.CoachId, "idx_training_plans_coach");
        builder.HasIndex(e => e.LearnerId, "idx_training_plans_learner");
        builder.HasIndex(e => e.Status, "idx_training_plans_status");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.GoalType).HasMaxLength(50);
        builder.Property(e => e.Overview).HasMaxLength(3000);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'draft'::character varying")
            .HasComment("draft | active | completed | cancelled");

        builder.HasOne(d => d.Booking)
            .WithOne(p => p.TrainingPlan)
            .HasForeignKey<TrainingPlan>(d => d.BookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_training_plans_booking");

        builder.HasOne(d => d.Learner)
            .WithMany(p => p.TrainingPlansAsLearner)
            .HasForeignKey(d => d.LearnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_training_plans_learner");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.TrainingPlans)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_training_plans_coach");
    }
}
