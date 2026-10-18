using Banking.Domain.Entities;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

/// <inheritdoc cref="ICustomerRepository"/>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly BankingDbContext _context;

    public CustomerRepository(BankingDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
        => _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

    public async Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        _context.Customers.Add(customer);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqliteErrors.IsUniqueConstraintViolation(ex))
        {
            _context.Entry(customer).State = EntityState.Detached;
            throw new DuplicateDocumentNumberException(
                $"A customer with document number '{customer.DocumentNumber}' already exists.", ex);
        }

        return customer;
    }
}
