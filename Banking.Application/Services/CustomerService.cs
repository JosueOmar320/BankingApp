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
    /// <summary>
    /// Service responsible for managing customer operations.
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerService"/> class.
        /// </summary>
        /// <param name="customerRepository">Repository for managing customers.</param>
        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        /// <summary>
        /// Creates a new customer in the system.
        /// </summary>
        /// <param name="customer">DTO containing the customer information.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>The newly created customer details.</returns>
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
