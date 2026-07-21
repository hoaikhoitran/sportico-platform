using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

/// <summary>Frontend navigation events — see PageView. NOT backend API requests (ApiRequestMetric).</summary>
public sealed class PageViewConfiguration : IEntityTypeConfiguration<PageView>
{
    public void Configure(EntityTypeBuilder<PageView> builder)
    {
        builder.ToTable("page_views", tb => tb.HasComment("Frontend navigation event submitted by the client, within a visitor session"));

        builder.HasKey(e => e.Id).HasName("page_views_pkey");

        builder.HasIndex(e => e.VisitorSessionId, "idx_page_views_session");
        builder.HasIndex(e => e.ViewedAt, "idx_page_views_viewed_at").IsDescending();
        builder.HasIndex(e => e.Path, "idx_page_views_path");
        builder.HasIndex(e => e.UserId, "idx_page_views_user");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Path).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200);
        builder.Property(e => e.Referrer).HasMaxLength(500);

        builder.HasOne(d => d.VisitorSession)
            .WithMany(p => p.PageViews)
            .HasForeignKey(d => d.VisitorSessionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_page_views_session");

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_page_views_user");
    }
}
