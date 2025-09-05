using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto customer, CancellationToken cancellationToken = default)
        {
            var result = await _customerRepository.AddCustomerAsync(customer.ToEntity(), cancellationToken);

            return new CustomerResponseDto {
                DocumentNumber = result.DocumentNumber, 
                FirstName = result.FirstName,
                LastName = result.LastName, 
                BirthDate = result.BirthDate, 
                MonthlyIncome = result.MonthlyIncome ,
                CustomerId = result.CustomerId,
                Gender = result.Gender,
            };
        } 

    }
}
