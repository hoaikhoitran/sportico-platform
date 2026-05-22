using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class CoachPackageConfiguration : IEntityTypeConfiguration<CoachPackage>
{
    public void Configure(EntityTypeBuilder<CoachPackage> builder)
    {
        builder.ToTable("coach_packages", tb => tb.HasComment("Lịch sử mua gói của coach"));

        builder.HasKey(e => e.Id).HasName("coach_packages_pkey");

        builder.HasIndex(e => e.CoachId, "idx_coach_packages_coach");
        builder.HasIndex(e => new { e.Status, e.EndDate }, "idx_coach_packages_status")
            .HasFilter("((status)::text = 'active'::text)");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.StartDate).HasDefaultValueSql("now()");
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying")
            .HasComment("pending | active | expired | cancelled");

        builder.HasOne(d => d.Coach)
            .WithMany(p => p.CoachPackages)
            .HasForeignKey(d => d.CoachId)
            .HasConstraintName("fk_coach_packages_coach");

        builder.HasOne(d => d.Package)
            .WithMany(p => p.CoachPackages)
            .HasForeignKey(d => d.PackageId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_coach_packages_package");
    }
}
