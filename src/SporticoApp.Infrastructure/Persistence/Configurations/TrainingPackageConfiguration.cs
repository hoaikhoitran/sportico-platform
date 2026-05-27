using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class TrainingPackageConfiguration : IEntityTypeConfiguration<TrainingPackage>
{
    public void Configure(EntityTypeBuilder<TrainingPackage> builder)
    {
        builder.ToTable("training_packages", tb => tb.HasComment("Training packages created by coaches"));

        builder.HasKey(e => e.Id).HasName("training_packages_pkey");

        builder.HasIndex(e => e.CoachId, "idx_training_packages_coach");
        builder.HasIndex(e => e.SportId, "idx_training_packages_sport");
        builder.HasIndex(e => e.Status, "idx_training_packages_status");
        builder.HasIndex(e => e.CreatedAt, "idx_training_packages_created_at").IsDescending();
        builder.HasIndex(e => e.Status, "idx_training_packages_published")
            .HasFilter("((status)::text = 'published'::text)");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(3000);
        builder.Property(e => e.Price).HasPrecision(12, 2);
        builder.Property(e => e.Location).HasMaxLength(255);
        builder.Property(e => e.Level).HasMaxLength(50);
        builder.Property(e => e.GoalType).HasMaxLength(50);
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying")
            .HasComment("pending | published | rejected | archived");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.TrainingPackages)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_training_packages_coach");

        builder.HasOne(d => d.Sport)
            .WithMany()
            .HasForeignKey(d => d.SportId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_training_packages_sport");
    }
}
