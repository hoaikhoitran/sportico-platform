using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
{
    public void Configure(EntityTypeBuilder<LearnerProfile> builder)
    {
        builder.ToTable("learner_profiles", tb => tb.HasComment("Hồ sơ học viên"));

        builder.HasKey(e => e.UserId).HasName("learner_profiles_pkey");

        builder.Property(e => e.UserId).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.User)
            .WithOne(p => p.LearnerProfile)
            .HasForeignKey<LearnerProfile>(d => d.UserId)
            .HasConstraintName("fk_learner_profiles_user");
    }
}
