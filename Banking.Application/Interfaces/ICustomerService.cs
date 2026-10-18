using Banking.Application.Dtos;
using Banking.Application.Exceptions;
using Banking.Domain.Exceptions;

namespace Banking.Application.Interfaces;

/// <summary>Customer use cases.</summary>
public interface ICustomerService
{
    /// <summary>Registers a new customer.</summary>
    /// <exception cref="DomainException">A value violates a business rule.</exception>
    /// <exception cref="DuplicateDocumentNumberException">The document number is already registered.</exception>
    Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default);

    /// <summary>Gets a customer by id.</summary>
    /// <exception cref="NotFoundException">The customer does not exist.</exception>
    Task<CustomerResponseDto> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);
}
