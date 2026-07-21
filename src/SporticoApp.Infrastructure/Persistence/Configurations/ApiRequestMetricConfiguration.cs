using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

/// <summary>Backend API usage telemetry — see ApiRequestMetric. NOT frontend page views (PageView).</summary>
public sealed class ApiRequestMetricConfiguration : IEntityTypeConfiguration<ApiRequestMetric>
{
    public void Configure(EntityTypeBuilder<ApiRequestMetric> builder)
    {
        builder.ToTable("api_request_metrics", tb => tb.HasComment("One tracked backend API request within a visitor session"));

        builder.HasKey(e => e.Id).HasName("api_request_metrics_pkey");

        builder.HasIndex(e => e.VisitorSessionId, "idx_api_request_metrics_session");
        builder.HasIndex(e => e.RequestedAt, "idx_api_request_metrics_requested_at").IsDescending();
        builder.HasIndex(e => e.Path, "idx_api_request_metrics_path");
        builder.HasIndex(e => e.UserId, "idx_api_request_metrics_user");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Path).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Method).HasMaxLength(10).IsRequired();

        builder.HasOne(d => d.VisitorSession)
            .WithMany(p => p.ApiRequestMetrics)
            .HasForeignKey(d => d.VisitorSessionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_api_request_metrics_session");

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_api_request_metrics_user");
    }
}
