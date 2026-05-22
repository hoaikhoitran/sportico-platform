using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", tb => tb.HasComment("Danh sách vai trò: admin, coach, learner"));

        builder.HasKey(e => e.Id).HasName("roles_pkey");

        builder.HasIndex(e => e.Name, "roles_name_key").IsUnique();

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Name).HasMaxLength(50);
    }
}
