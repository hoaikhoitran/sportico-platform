using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins", tb =>
            tb.HasComment("Links a Sportico user to an identity at an external provider (Google)"));

        builder.HasKey(e => e.Id).HasName("user_external_logins_pkey");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Provider).HasMaxLength(30).IsRequired();
        builder.Property(e => e.ProviderSubject).HasMaxLength(255).IsRequired();
        // citext: provider emails are compared case-insensitively, like users.email.
        builder.Property(e => e.ProviderEmail).HasColumnType("citext");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        // One provider identity can belong to exactly one Sportico user.
        builder.HasIndex(e => new { e.Provider, e.ProviderSubject }, "uq_user_external_logins_provider_subject")
            .IsUnique();

        // One Sportico user can hold at most one link per provider.
        builder.HasIndex(e => new { e.UserId, e.Provider }, "uq_user_external_logins_user_provider")
            .IsUnique();

        builder.HasIndex(e => e.UserId, "idx_user_external_logins_user");

        builder.HasOne(d => d.User)
            .WithMany(p => p.ExternalLogins)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_external_logins_user");
    }
}
