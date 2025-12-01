using Banking.Application.Dtos;
using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Interfaces
{
    /// <summary>
    /// Service interface for managing customer operations.
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Creates a new customer in the system.
        /// </summary>
        /// <param name="customer">DTO containing the customer information.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The newly created customer details.</returns>
        Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a customer by their unique ID.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The <see cref="Customer"/> entity or null if not found.</returns>
        Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);
    }
}
