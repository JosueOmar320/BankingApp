using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);
        Task<Customer> AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
    }
}
