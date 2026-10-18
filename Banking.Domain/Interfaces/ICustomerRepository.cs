using Banking.Domain.Entities;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Interfaces;

/// <summary>Persistence operations for <see cref="Customer"/>.</summary>
public interface ICustomerRepository
{
    /// <summary>Gets a customer by id, or <see langword="null"/> when it does not exist.</summary>
    Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new customer.</summary>
    /// <exception cref="DuplicateDocumentNumberException">Another customer has the same document number.</exception>
    Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
}
