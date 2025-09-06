using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for handling customer operations in the database.
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly BankingDbContext _context;

        /// <summary>
        /// Constructor that receives the database context.
        /// </summary>
        /// <param name="context">Instance of <see cref="BankingDbContext"/> for database access.</param>
        public CustomerRepository(BankingDbContext context)
        {
            _context = context;
        }

        // <summary>
        /// Adds a new customer to the database.
        /// </summary>
        /// <param name="customer">The <see cref="Customer"/> entity to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The added customer.</returns>
        public async Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
           await _context.Customers.AddAsync(customer, cancellationToken);  
           await _context.SaveChangesAsync(cancellationToken);
           return customer;
        }

        /// <summary>
        /// Gets a customer by their unique ID.
        /// </summary>
        /// <param name="customerId">The ID of the customer.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The <see cref="Customer"/> entity or null if not found.</returns>
        public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
            => _context.Customers.FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
    }
}
