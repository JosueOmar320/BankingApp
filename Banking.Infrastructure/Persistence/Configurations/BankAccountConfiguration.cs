using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

internal sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(a => a.BankAccountId);
        builder.Property(a => a.BankAccountId).ValueGeneratedOnAdd();

        builder.Property(a => a.AccountNumber).IsRequired().HasMaxLength(32);
        builder.Property(a => a.Balance).IsRequired();
        builder.Property(a => a.InterestRate).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired().HasConversion<UtcDateTimeConverter>();

        // Optimistic concurrency: an UPDATE only succeeds if Version is still the value we read.
        builder.Property(a => a.Version).IsRequired().IsConcurrencyToken();

        builder.HasIndex(a => a.AccountNumber).IsUnique();

        // Financial records must never disappear because a customer row was deleted.
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Persist through the backing field so the aggregate controls the collection.
        builder.Navigation(a => a.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
