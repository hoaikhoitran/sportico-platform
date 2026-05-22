using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", tb => tb.HasComment("Many-to-many giữa users và roles"));

        builder.HasKey(e => new { e.UserId, e.RoleId }).HasName("user_roles_pkey");

        builder.HasIndex(e => e.RoleId, "idx_user_roles_role");

        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Role)
            .WithMany(p => p.UserRoles)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_roles_role");

        builder.HasOne(d => d.User)
            .WithMany(p => p.UserRoles)
            .HasForeignKey(d => d.UserId)
            .HasConstraintName("fk_user_roles_user");
    }
}
