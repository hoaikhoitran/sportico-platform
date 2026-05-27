using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class ProgressCheckInConfiguration : IEntityTypeConfiguration<ProgressCheckIn>
{
    public void Configure(EntityTypeBuilder<ProgressCheckIn> builder)
    {
        builder.ToTable("progress_check_ins", tb => tb.HasComment("Progress check-ins for bookings"));

        builder.HasKey(e => e.Id).HasName("progress_check_ins_pkey");

        builder.HasIndex(e => new { e.BookingId, e.CreatedAt }, "idx_progress_check_ins_booking_created_at")
            .IsDescending(false, true);

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.WeightKg).HasPrecision(6, 2);
        builder.Property(e => e.BodyFatPercent).HasPrecision(6, 2);
        builder.Property(e => e.WaistCm).HasPrecision(6, 2);
        builder.Property(e => e.EnergyLevel).HasMaxLength(50);
        builder.Property(e => e.SleepQuality).HasMaxLength(50);
        builder.Property(e => e.LearnerNote).HasMaxLength(2000);
        builder.Property(e => e.CoachFeedback).HasMaxLength(2000);

        builder.HasOne(d => d.Booking)
            .WithMany()
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_progress_check_ins_booking");

        builder.HasOne(d => d.Learner)
            .WithMany(p => p.ProgressCheckInsAsLearner)
            .HasForeignKey(d => d.LearnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_progress_check_ins_learner");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.ProgressCheckIns)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_progress_check_ins_coach");
    }
}
