using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", tb => tb.HasComment("Đánh giá từ learner cho coach"));

        builder.HasKey(e => e.Id).HasName("reviews_pkey");

        builder.HasIndex(e => e.CoachId, "idx_reviews_coach");
        builder.HasIndex(e => e.CreatedAt, "idx_reviews_created_at").IsDescending();
        builder.HasIndex(e => e.learner_id, "idx_reviews_learner");
        builder.HasIndex(e => e.PostId, "idx_reviews_post").HasFilter("(post_id IS NOT NULL)");
        builder.HasIndex(e => new { e.CoachId, e.learner_id }, "uq_reviews_pair").IsUnique();
        builder.HasIndex(e => e.BookingId, "idx_reviews_booking").HasFilter("(booking_id IS NOT NULL)");
        // Public listing path: active reviews for a coach, newest first.
        builder.HasIndex(e => new { e.CoachId, e.Status, e.CreatedAt }, "idx_reviews_coach_status_created");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'::character varying")
            .HasComment("active | hidden | deleted");
        builder.Property(e => e.ModerationReason).HasMaxLength(500);

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.Reviews)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_reviews_coach");

        builder.HasOne(d => d.learner)
            .WithMany(p => p.Reviews)
            .HasForeignKey(d => d.learner_id)
            .HasConstraintName("fk_reviews_learner");

        builder.HasOne(d => d.Post)
            .WithMany(p => p.Reviews)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_reviews_post");

        builder.HasOne(d => d.Booking)
            .WithMany()
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_reviews_booking");
    }
}
