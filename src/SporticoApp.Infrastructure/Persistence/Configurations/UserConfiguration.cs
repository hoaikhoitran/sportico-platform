using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", tb => tb.HasComment("Bảng người dùng cốt lõi"));

        builder.HasKey(e => e.Id).HasName("users_pkey");

        builder.HasIndex(e => e.CreatedAt, "idx_users_created_at").IsDescending();
        builder.HasIndex(e => e.Status, "idx_users_status")
            .HasFilter("((status)::text <> 'active'::text)");
        builder.HasIndex(e => e.Email, "users_email_key").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Email).HasColumnType("citext");
        builder.Property(e => e.EmailVerificationToken)
            .HasColumnName("email_verification_token")
            .HasMaxLength(255);
        builder.Property(e => e.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(255);
        builder.Property(e => e.RefreshTokenExpiresAt)
            .HasColumnName("refresh_token_expires_at");
        builder.Property(e => e.FullName).HasMaxLength(150);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'active'::character varying")
            .HasComment("active | inactive | banned | pending");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
    }
}
