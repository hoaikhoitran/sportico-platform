using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions", tb => tb.HasComment("Log raw response từ payment gateway (audit trail)"));

        builder.HasKey(e => e.Id).HasName("payment_transactions_pkey");

        builder.HasIndex(e => e.payment_id, "idx_payment_transactions_payment");

        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.Payment)
            .WithMany(p => p.PaymentTransactions)
            .HasForeignKey(d => d.payment_id)
            .HasConstraintName("fk_payment_transactions_payment");
    }
}
