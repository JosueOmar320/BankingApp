using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Persistence;
using Banking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Tests.Infrastructure.Repositories
{
    public class CustomerRepositoryTests
    {
        private BankingDbContext _context;
        private CustomerRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BankingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new BankingDbContext(options);
            _repository = new CustomerRepository(_context);
        }

        [TearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [Test]
        public async Task AddCustomerAsync_ShouldPersistCustomer()
        {
            var customer = new Customer
            {
                FirstName = "Juan",
                LastName = "Perez",
                DocumentNumber = "12345",
                BirthDate = new DateOnly(1990, 1, 1),
                MonthlyIncome = 2000,
                Gender = Gender.M
            };

            var result = await _repository.AddCustomerAsync(customer);

            Assert.NotNull(result);
            Assert.That(result.FirstName, Is.EqualTo("Juan"));
            Assert.That(await _context.Customers.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetCustomerByIdAsync_ShouldReturnCustomer()
        {
            var customer = new Customer
            {
                FirstName = "Ana",
                LastName = "Lopez",
                DocumentNumber = "67890",
                BirthDate = new DateOnly(1995, 5, 10),
                MonthlyIncome = 3000,
                Gender = Gender.F
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var result = await _repository.GetCustomerByIdAsync(customer.CustomerId);

            Assert.NotNull(result);
            Assert.That(result.FirstName, Is.EqualTo("Ana"));
        }
    }
}
