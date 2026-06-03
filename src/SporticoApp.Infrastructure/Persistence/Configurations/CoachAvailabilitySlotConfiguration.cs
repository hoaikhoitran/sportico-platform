using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachAvailabilitySlotConfiguration : IEntityTypeConfiguration<CoachAvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<CoachAvailabilitySlot> builder)
    {
        builder.ToTable("coach_availability_slots",
            tb => tb.HasComment("Time slots a coach publishes as bookable"));

        builder.HasKey(e => e.Id).HasName("coach_availability_slots_pkey");

        builder.HasIndex(e => e.CoachId, "idx_coach_availability_slots_coach");
        builder.HasIndex(e => e.Status, "idx_coach_availability_slots_status");
        builder.HasIndex(e => e.StartTime, "idx_coach_availability_slots_start_time");
        builder.HasIndex(e => new { e.CoachId, e.StartTime }, "idx_coach_availability_slots_coach_start");

        // Prevent a coach from creating two identical slots
        builder.HasIndex(e => new { e.CoachId, e.StartTime, e.EndTime },
            "uq_coach_availability_slots_coach_time").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'available'::character varying")
            .HasComment("available | booked | cancelled | expired");

        // Group-slot capacity. Existing rows backfill to 1 (private slot) via the column default.
        builder.Property(e => e.MaxParticipants)
            .HasDefaultValue(1)
            .HasComment("Maximum learners that can book this slot");

        // Optimistic concurrency token (same pattern as CoachWallet.Version): EF adds
        // `WHERE version = @old` to every UPDATE, so two concurrent bookings of the last seat
        // cannot both commit.
        builder.Property(e => e.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.Property(e => e.Location).HasMaxLength(255);
        builder.Property(e => e.MeetingUrl).HasMaxLength(1000);
        builder.Property(e => e.Note).HasMaxLength(2000);

        builder.HasOne(d => d.Coach)
            .WithMany()
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_coach_availability_slots_coach");
    }
}
