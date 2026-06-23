using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingPackageSessionSlotConfiguration
    : IEntityTypeConfiguration<TrainingPackageSessionSlot>
{
    public void Configure(EntityTypeBuilder<TrainingPackageSessionSlot> builder)
    {
        builder.ToTable("training_package_session_slots",
            tb => tb.HasComment("Fixed schedule of sessions defined for a training package"));

        builder.HasKey(e => e.Id).HasName("training_package_session_slots_pkey");

        builder.HasIndex(e => e.TrainingPackageId, "idx_training_package_session_slots_package");
        builder.HasIndex(e => e.Status, "idx_training_package_session_slots_status");

        // Session numbers are unique within a package (1..SessionCount).
        builder.HasIndex(e => new { e.TrainingPackageId, e.SessionNumber },
            "uq_training_package_session_slots_package_number").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Level).HasMaxLength(50);
        builder.Property(e => e.Location).HasMaxLength(255);
        builder.Property(e => e.MeetingUrl).HasMaxLength(1000);
        builder.Property(e => e.Note).HasMaxLength(2000);

        builder.Property(e => e.MaxParticipants)
            .HasDefaultValue(1)
            .HasComment("Maximum learners that can buy a seat on this session");
        builder.Property(e => e.BookedParticipants).HasDefaultValue(0);

        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'open'::character varying")
            .HasComment("open | full | cancelled");

        // Optimistic concurrency token (same pattern as CoachAvailabilitySlot.Version): two learners
        // buying the last seat of the same slot concurrently cannot both commit.
        builder.Property(e => e.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        builder.HasOne(d => d.TrainingPackage)
            .WithMany(p => p.SessionSlots)
            .HasForeignKey(d => d.TrainingPackageId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_training_package_session_slots_package");
    }
}
