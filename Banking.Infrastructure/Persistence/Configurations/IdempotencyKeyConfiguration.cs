using Banking.Domain.Entities;
using Banking.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("IdempotencyKeys");

        // One key per customer: the same key sent by two customers is two independent operations.
        builder.HasKey(k => new { k.CustomerId, k.Key });

        builder.Property(k => k.Key).IsRequired().HasMaxLength(IdempotencyKey.MaxKeyLength);
        builder.Property(k => k.RequestHash).IsRequired().HasMaxLength(IdempotencyKey.RequestHashLength);
        builder.Property(k => k.ResponseContentType).HasMaxLength(IdempotencyKey.MaxContentTypeLength);
        builder.Property(k => k.CreatedAt).IsRequired().HasConversion<UtcDateTimeConverter>();

        builder.Ignore(k => k.IsCompleted);

        // The cleanup deletes by age.
        builder.HasIndex(k => k.CreatedAt);

        // Keys are not financial records: they go with the customer.
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(k => k.CustomerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
