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

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying")
            .HasComment("pending | reviewing | resolved | rejected");

        builder.HasOne(d => d.reporter)
            .WithMany(p => p.ReportsAsReporter)
            .HasForeignKey(d => d.reporter_id)
            .HasConstraintName("fk_reports_reporter");

        builder.HasOne(d => d.target_user)
            .WithMany(p => p.ReportsAsTargetUser)
            .HasForeignKey(d => d.target_user_id)
            .HasConstraintName("fk_reports_target");
    }
}
