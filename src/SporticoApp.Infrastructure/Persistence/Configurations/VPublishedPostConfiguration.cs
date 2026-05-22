using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class VPublishedPostConfiguration : IEntityTypeConfiguration<VPublishedPost>
{
    public void Configure(EntityTypeBuilder<VPublishedPost> builder)
    {
        builder.HasNoKey();
        builder.ToView("v_published_posts");

        builder.Property(e => e.CoachName).HasMaxLength(150);
        builder.Property(e => e.CoachRating).HasPrecision(3, 2);
        builder.Property(e => e.Location).HasMaxLength(255);
        builder.Property(e => e.Price).HasPrecision(12, 2);
        builder.Property(e => e.SportName).HasMaxLength(100);
        builder.Property(e => e.SportSlug).HasMaxLength(120);
        builder.Property(e => e.Title).HasMaxLength(255);
    }
}
