using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings", tb => tb.HasComment("Bookings for training package purchases"));

        builder.HasKey(e => e.Id).HasName("bookings_pkey");

        builder.HasIndex(e => e.LearnerId, "idx_bookings_learner");
        builder.HasIndex(e => e.CoachId, "idx_bookings_coach");
        builder.HasIndex(e => e.TrainingPackageId, "idx_bookings_training_package");
        builder.HasIndex(e => e.Status, "idx_bookings_status");
        builder.HasIndex(e => e.CreatedAt, "idx_bookings_created_at").IsDescending();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.TotalAmount).HasPrecision(12, 2);
        builder.Property(e => e.PlatformFeeRate).HasPrecision(5, 4);
        builder.Property(e => e.PlatformFeeAmount).HasPrecision(12, 2);
        builder.Property(e => e.CoachReceiveAmount).HasPrecision(12, 2);
        builder.Property(e => e.PerSessionCoachAmount).HasPrecision(12, 2);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending_payment'::character varying")
            .HasComment("pending_payment | active | completed | cancelled | refunded");
        builder.Property(e => e.CompletedSessions).HasDefaultValue(0);
        builder.Property(e => e.TotalSessions).HasDefaultValue(0);

        builder.HasOne(d => d.Learner)
            .WithMany(p => p.BookingsAsLearner)
            .HasForeignKey(d => d.LearnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_bookings_learner");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_bookings_coach");

        builder.HasOne(d => d.TrainingPackage)
            .WithMany(p => p.Bookings)
            .HasForeignKey(d => d.TrainingPackageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_bookings_training_package");
    }
}
