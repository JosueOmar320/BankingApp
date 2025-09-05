using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        public Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Customer> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
