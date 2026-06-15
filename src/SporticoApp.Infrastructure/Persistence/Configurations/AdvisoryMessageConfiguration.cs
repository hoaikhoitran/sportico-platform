using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class AdvisoryMessageConfiguration : IEntityTypeConfiguration<AdvisoryMessage>
{
    public void Configure(EntityTypeBuilder<AdvisoryMessage> builder)
    {
        builder.ToTable("advisory_messages", tb =>
        {
            tb.HasComment("Turns within an advisory conversation");

            tb.HasCheckConstraint(
                "chk_advisory_messages_sender",
                "sender IN ('user','assistant')");
        });

        builder.HasKey(e => e.Id).HasName("advisory_messages_pkey");

        builder.HasIndex(e => new { e.ConversationId, e.CreatedAt }, "idx_advisory_messages_conversation")
            .IsDescending(false, true);

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Sender)
            .HasMaxLength(20)
            .HasComment("user | assistant");

        builder.HasOne(d => d.Conversation)
            .WithMany(p => p.Messages)
            .HasForeignKey(d => d.ConversationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_advisory_messages_conversation");
    }
}
