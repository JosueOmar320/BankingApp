using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for managing customer entities in the persistence layer.
    /// </summary>
    public interface ICustomerRepository
    {
        // <summary>
        /// Adds a new customer to the database.
        /// </summary>
        /// <param name="customer">The <see cref="Customer"/> entity to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The added customer.</returns>
        Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a customer by their unique ID.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The <see cref="Customer"/> entity or null if not found.</returns>
        Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
    }
}
