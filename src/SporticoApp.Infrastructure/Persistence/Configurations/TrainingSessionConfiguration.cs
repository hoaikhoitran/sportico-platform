using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.ToTable("training_sessions", tb => tb.HasComment("Training session schedule for bookings"));

        builder.HasKey(e => e.Id).HasName("training_sessions_pkey");

        builder.HasIndex(e => e.BookingId, "idx_training_sessions_booking");
        builder.HasIndex(e => e.LearnerId, "idx_training_sessions_learner");
        builder.HasIndex(e => e.CoachId, "idx_training_sessions_coach");
        builder.HasIndex(e => e.Status, "idx_training_sessions_status");
        builder.HasIndex(e => new { e.CoachId, e.StartTime, e.EndTime }, "idx_training_sessions_coach_time");
        builder.HasIndex(e => new { e.LearnerId, e.StartTime, e.EndTime }, "idx_training_sessions_learner_time");

        // A single availability slot can back only ONE non-cancelled session. Cancelled (and
        // missed) sessions release the slot, so they are excluded from the uniqueness filter.
        // This is the DB-level guard against concurrent double-booking of the same slot.
        builder.HasIndex(e => e.AvailabilitySlotId, "uq_training_sessions_active_slot")
            .IsUnique()
            .HasFilter("availability_slot_id IS NOT NULL AND status IN ('requested', 'scheduled', 'completed')");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.MeetingUrl).HasMaxLength(1000);
        builder.Property(e => e.Location).HasMaxLength(255);
        builder.Property(e => e.LearnerNote).HasMaxLength(2000);
        builder.Property(e => e.CoachNote).HasMaxLength(2000);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'requested'::character varying")
            .HasComment("requested | scheduled | completed | cancelled | missed");

        builder.HasOne(d => d.Booking)
            .WithMany(p => p.TrainingSessions)
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_training_sessions_booking");

        builder.HasOne(d => d.Learner)
            .WithMany(p => p.TrainingSessionsAsLearner)
            .HasForeignKey(d => d.LearnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_training_sessions_learner");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.TrainingSessions)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_training_sessions_coach");

        builder.HasOne(d => d.AvailabilitySlot)
            .WithMany()
            .HasForeignKey(d => d.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_training_sessions_availability_slot");
    }
}
