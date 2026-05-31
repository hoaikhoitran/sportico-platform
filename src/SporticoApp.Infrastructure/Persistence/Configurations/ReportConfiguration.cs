using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports", tb => tb.HasComment("Báo cáo vi phạm"));

        builder.HasKey(e => e.Id).HasName("reports_pkey");

        builder.HasIndex(e => e.Status, "idx_reports_status")
            .HasFilter("((status)::text = ANY ((ARRAY['pending'::character varying, 'reviewing'::character varying])::text[]))");
        builder.HasIndex(e => e.target_user_id, "idx_reports_target");
        // Lookup reports by what they point at (review moderation queue).
        builder.HasIndex(e => new { e.TargetType, e.TargetId }, "idx_reports_target_entity");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying")
            .HasComment("pending | reviewing | resolved | rejected");
        builder.Property(e => e.TargetType)
            .HasMaxLength(20)
            .HasDefaultValueSql("'user'::character varying")
            .HasComment("user | review");
        builder.Property(e => e.ActionTaken)
            .HasMaxLength(30)
            .HasComment("none | review_hidden | review_deleted");
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ResolutionNote).HasMaxLength(1000);

        builder.HasOne(d => d.reporter)
            .WithMany(p => p.ReportsAsReporter)
            .HasForeignKey(d => d.reporter_id)
            .HasConstraintName("fk_reports_reporter");

        // target_user_id is now optional (review reports leave it null).
        builder.HasOne(d => d.target_user)
            .WithMany(p => p.ReportsAsTargetUser)
            .HasForeignKey(d => d.target_user_id)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_reports_target");
    }
}
