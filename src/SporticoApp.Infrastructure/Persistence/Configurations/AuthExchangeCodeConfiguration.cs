using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class AuthExchangeCodeConfiguration : IEntityTypeConfiguration<AuthExchangeCode>
{
    public void Configure(EntityTypeBuilder<AuthExchangeCode> builder)
    {
        builder.ToTable("auth_exchange_codes", tb =>
            tb.HasComment("Short-lived single-use codes exchanged for Sportico tokens after external login"));

        builder.HasKey(e => e.Id).HasName("auth_exchange_codes_pkey");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        // SHA-256 hex = exactly 64 chars. The plaintext code is never stored.
        builder.Property(e => e.CodeHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(e => e.CodeHash, "uq_auth_exchange_codes_code_hash").IsUnique();
        builder.HasIndex(e => e.ExpiresAt, "idx_auth_exchange_codes_expires_at");

        builder.HasOne(d => d.User)
            .WithMany(p => p.AuthExchangeCodes)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_auth_exchange_codes_user");
    }
}
