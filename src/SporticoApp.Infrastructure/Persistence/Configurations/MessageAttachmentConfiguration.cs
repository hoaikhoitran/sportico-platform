using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("message_attachments", tb => tb.HasComment("File đính kèm tin nhắn (image, video, doc...)"));

        builder.HasKey(e => e.Id).HasName("message_attachments_pkey");

        builder.HasIndex(e => e.MessageId, "idx_message_attachments_message");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.FileType).HasMaxLength(50);

        builder.HasOne(d => d.Message)
            .WithMany(p => p.MessageAttachments)
            .HasForeignKey(d => d.MessageId)
            .HasConstraintName("fk_message_attachments_message");
    }
}
