using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class VCoachConfiguration : IEntityTypeConfiguration<VCoach>
{
    public void Configure(EntityTypeBuilder<VCoach> builder)
    {
        builder.HasNoKey();
        builder.ToView("v_coaches");

        builder.Property(e => e.Email).HasColumnType("citext");
        builder.Property(e => e.FullName).HasMaxLength(150);
        builder.Property(e => e.Headline).HasMaxLength(255);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Rating).HasPrecision(3, 2);
        builder.Property(e => e.Sports).HasColumnType("character varying[]");
        builder.Property(e => e.Status).HasMaxLength(20);
    }
}
