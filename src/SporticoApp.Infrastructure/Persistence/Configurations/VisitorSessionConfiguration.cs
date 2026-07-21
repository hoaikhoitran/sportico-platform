using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class VisitorSessionConfiguration : IEntityTypeConfiguration<VisitorSession>
{
    public void Configure(EntityTypeBuilder<VisitorSession> builder)
    {
        builder.ToTable("visitor_sessions", tb => tb.HasComment("One browsing session by an anonymous or logged-in visitor"));

        builder.HasKey(e => e.Id).HasName("visitor_sessions_pkey");

        // Primary lookup: "find this visitor's still-open session" (VisitorTrackingRepository).
        builder.HasIndex(e => new { e.VisitorId, e.LastSeenAt }, "idx_visitor_sessions_visitor_last_seen");

        // Dashboard time-bucketed charts group by FirstSeenAt; LastSeenAt powers "active now".
        builder.HasIndex(e => e.FirstSeenAt, "idx_visitor_sessions_first_seen").IsDescending();
        builder.HasIndex(e => e.LastSeenAt, "idx_visitor_sessions_last_seen").IsDescending();

        // Breakdown charts group by these low-cardinality columns.
        builder.HasIndex(e => e.Device, "idx_visitor_sessions_device");
        builder.HasIndex(e => e.Browser, "idx_visitor_sessions_browser");
        builder.HasIndex(e => e.Country, "idx_visitor_sessions_country");
        builder.HasIndex(e => e.UserId, "idx_visitor_sessions_user");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IpHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.Device).HasMaxLength(20);
        builder.Property(e => e.Browser).HasMaxLength(50);
        builder.Property(e => e.Os).HasMaxLength(50);
        builder.Property(e => e.Country).HasMaxLength(100);
        builder.Property(e => e.PageViewCount).HasDefaultValue(0);
        builder.Property(e => e.ApiRequestCount).HasDefaultValue(0);
        builder.Property(e => e.DurationSeconds).HasDefaultValue(0);
        builder.Property(e => e.IsNewVisitor).HasDefaultValue(true);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_visitor_sessions_user");
    }
}
