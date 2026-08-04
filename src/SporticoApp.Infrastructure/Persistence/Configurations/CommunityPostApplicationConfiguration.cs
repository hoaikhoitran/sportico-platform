using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CommunityPostApplicationConfiguration : IEntityTypeConfiguration<CommunityPostApplication>
{
    public void Configure(EntityTypeBuilder<CommunityPostApplication> builder)
    {
        builder.ToTable("community_post_applications", tb =>
            tb.HasComment("A user's request to join a recruitment-type community post"));

        builder.HasKey(e => e.Id).HasName("community_post_applications_pkey");

        builder.HasIndex(e => new { e.PostId, e.ApplicantId }, "uq_community_post_applications_post_applicant").IsUnique();
        builder.HasIndex(e => new { e.PostId, e.Status }, "idx_community_post_applications_post_status");
        builder.HasIndex(e => e.ApplicantId, "idx_community_post_applications_applicant");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Message).HasMaxLength(500);
        builder.Property(e => e.Status)
            .HasMaxLength(10)
            .HasDefaultValueSql("'pending'::character varying")
            .IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Post)
            .WithMany(p => p.Applications)
            .HasForeignKey(d => d.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_community_post_applications_post");

        builder.HasOne(d => d.Applicant)
            .WithMany()
            .HasForeignKey(d => d.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_community_post_applications_applicant");
    }
}
