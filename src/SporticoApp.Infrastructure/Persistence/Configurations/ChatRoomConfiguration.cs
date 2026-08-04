using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("chat_rooms", tb => tb.HasComment("Phòng chat 1-1 giữa 2 user"));

        builder.HasKey(e => e.Id).HasName("chat_rooms_pkey");

        builder.HasIndex(e => e.User1Id, "idx_chat_rooms_user1");
        builder.HasIndex(e => e.User2Id, "idx_chat_rooms_user2");
        builder.HasIndex(e => new { e.User1Id, e.User2Id }, "uq_chat_rooms_pair").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        // New rooms are "pending" until the target accepts; existing rows are backfilled to
        // "active" by the migration so coach<->learner chat keeps working unchanged.
        builder.Property(e => e.Status)
            .HasMaxLength(10)
            .HasDefaultValueSql("'active'::character varying")
            .IsRequired();
        builder.Property(e => e.SourceType).HasMaxLength(30);

        builder.HasIndex(e => e.Status, "idx_chat_rooms_status");

        builder.HasOne(d => d.User1)
            .WithMany(p => p.ChatRoomsAsUser1)
            .HasForeignKey(d => d.User1Id)
            .HasConstraintName("fk_chat_rooms_user1");

        builder.HasOne(d => d.User2)
            .WithMany(p => p.ChatRoomsAsUser2)
            .HasForeignKey(d => d.User2Id)
            .HasConstraintName("fk_chat_rooms_user2");
    }
}
