using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", tb =>
        {
            tb.HasComment("Thông báo cho user");

            // Keep this list in sync with NotificationTypeConstants. The constraint is named
            // chk_notifications_type to match the value already present in the configured
            // PostgreSQL database (migration UpdateNotificationTypeCheckConstraint drops the
            // old definition first, then re-adds this one).
            tb.HasCheckConstraint(
                "chk_notifications_type",
                "type IN ('message','review','follow','payment','package','post','system','report','booking','training_package','training_session','training_plan','wallet')");
        });

        builder.HasKey(e => e.Id).HasName("notifications_pkey");

        builder.HasIndex(e => new { e.UserId, e.IsRead }, "idx_notifications_unread")
            .HasFilter("(is_read = false)");
        builder.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_notifications_user")
            .IsDescending(false, true);

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsRead).HasDefaultValue(false);
        builder.Property(e => e.Title).HasMaxLength(255);
        builder.Property(e => e.Type)
            .HasMaxLength(50)
            .HasComment("message | review | follow | payment | package | post | system | report | booking | training_package | training_session | training_plan | wallet");

        builder.HasOne(d => d.User)
            .WithMany(p => p.Notifications)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("fk_notifications_user");
    }
}
