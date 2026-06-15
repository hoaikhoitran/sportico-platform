using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class AdvisoryConversationConfiguration : IEntityTypeConfiguration<AdvisoryConversation>
{
    public void Configure(EntityTypeBuilder<AdvisoryConversation> builder)
    {
        builder.ToTable("advisory_conversations", tb =>
        {
            tb.HasComment("AI advisory chatbot conversations started by a learner or admin");

            tb.HasCheckConstraint(
                "chk_advisory_conversations_initiator_role",
                "initiator_role IN ('learner','admin')");
        });

        builder.HasKey(e => e.Id).HasName("advisory_conversations_pkey");

        builder.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_advisory_conversations_user")
            .IsDescending(false, true);

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.InitiatorRole)
            .HasMaxLength(20)
            .HasComment("learner | admin");
        builder.Property(e => e.Title).HasMaxLength(255);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_advisory_conversations_user");
    }
}
