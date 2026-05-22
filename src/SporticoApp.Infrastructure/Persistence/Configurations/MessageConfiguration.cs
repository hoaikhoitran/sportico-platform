using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", tb => tb.HasComment("Tin nhắn trong phòng chat"));

        builder.HasKey(e => e.Id).HasName("messages_pkey");

        builder.HasIndex(e => new { e.RoomId, e.SentAt }, "idx_messages_room")
            .IsDescending(false, true);
        builder.HasIndex(e => e.SenderId, "idx_messages_sender");
        builder.HasIndex(e => new { e.RoomId, e.IsRead }, "idx_messages_unread")
            .HasFilter("(is_read = false)");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.IsRead).HasDefaultValue(false);
        builder.Property(e => e.SentAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Room)
            .WithMany(p => p.Messages)
            .HasForeignKey(d => d.RoomId)
            .HasConstraintName("fk_messages_room");

        builder.HasOne(d => d.Sender)
            .WithMany(p => p.Messages)
            .HasForeignKey(d => d.SenderId)
            .HasConstraintName("fk_messages_sender");
    }
}
