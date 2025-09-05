using Banking.Application.Dtos;
using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerDto> AddCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default);
    }
}
