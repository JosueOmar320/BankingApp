using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.TransactionId);
        builder.Property(t => t.TransactionId).ValueGeneratedOnAdd();

        builder.Property(t => t.Amount).IsRequired();
        builder.Property(t => t.BalanceAfter).IsRequired();
        builder.Property(t => t.TransactionDate).IsRequired().HasConversion<UtcDateTimeConverter>();
        builder.Property(t => t.TransactionType).IsRequired().HasConversion<string>().HasMaxLength(20);

        // The ledger is read per account ordered by date.
        builder.HasIndex(t => new { t.BankAccountId, t.TransactionDate });
    }
}
