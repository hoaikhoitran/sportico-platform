using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> entity)
    {
        entity.ToTable("refresh_tokens");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id");

        entity.Property(x => x.UserId)
            .HasColumnName("user_id");

        entity.Property(x => x.Token)
            .HasColumnName("token")
            .HasMaxLength(255)
            .IsRequired();

        entity.HasIndex(x => x.Token)
            .IsUnique();

        entity.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        entity.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        // FK relationship
        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}