using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.CustomerId);
        builder.Property(c => c.CustomerId).ValueGeneratedOnAdd();

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(Customer.MaxNameLength);
        builder.Property(c => c.LastName).HasMaxLength(Customer.MaxNameLength);
        builder.Property(c => c.DocumentNumber).IsRequired().HasMaxLength(Customer.MaxDocumentNumberLength);
        builder.Property(c => c.BirthDate).IsRequired();
        builder.Property(c => c.Gender).IsRequired().HasConversion<string>().HasMaxLength(1);
        builder.Property(c => c.MonthlyIncome).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired().HasConversion<UtcDateTimeConverter>();

        builder.Ignore(c => c.FullName);

        // Two customers can never share an identification document.
        builder.HasIndex(c => c.DocumentNumber).IsUnique();
    }
}
