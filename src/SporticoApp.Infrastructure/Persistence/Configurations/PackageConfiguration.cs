using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages", tb => tb.HasComment("Gói dịch vụ dành cho coach (basic, pro, premium...)"));

        builder.HasKey(e => e.Id).HasName("packages_pkey");

        builder.HasIndex(e => e.IsActive, "idx_packages_active")
            .HasFilter("(is_active = true)");
        builder.HasIndex(e => e.Name, "packages_name_key").IsUnique();

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.Name).HasMaxLength(100);
        builder.Property(e => e.Price).HasPrecision(12, 2);
    }
}
