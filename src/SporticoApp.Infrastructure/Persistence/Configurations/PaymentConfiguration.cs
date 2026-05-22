using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", tb => tb.HasComment("Giao dịch thanh toán"));

        builder.HasKey(e => e.Id).HasName("payments_pkey");

        builder.HasIndex(e => e.CreatedAt, "idx_payments_created_at").IsDescending();
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_payments_reference");
        builder.HasIndex(e => e.Status, "idx_payments_status");
        builder.HasIndex(e => e.UserId, "idx_payments_user");
        builder.HasIndex(e => e.TransactionCode, "payments_transaction_code_key").IsUnique();

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(e => e.Method).HasMaxLength(50);
        builder.Property(e => e.ReferenceId).HasComment("ID của đối tượng được thanh toán (vd: coach_packages.id)");
        builder.Property(e => e.ReferenceType)
            .HasMaxLength(50)
            .HasComment("Polymorphic: liên kết với coach_package hoặc đối tượng khác");
        builder.Property(e => e.Status)
            .HasMaxLength(20)
            .HasDefaultValueSql("'pending'::character varying");
        builder.Property(e => e.TransactionCode).HasMaxLength(100);

        builder.HasOne(d => d.User)
            .WithMany(p => p.Payments)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_payments_user");
    }
}
