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
    public class CustomerRepository : ICustomerRepository
    {
        private readonly BankingDbContext _context;
        public CustomerRepository(BankingDbContext context)
        {
            _context = context;
        }
        public async Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
           await _context.Customers.AddAsync(customer, cancellationToken);  
           await _context.SaveChangesAsync(cancellationToken);
           return customer;
        }

        public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
            => _context.Customers.FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
    }
}
