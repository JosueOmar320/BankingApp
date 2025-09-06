using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Tests.Application.Services
{
    public class CustomerServiceTests
    {
        private Mock<ICustomerRepository> _customerRepositoryMock;
        private CustomerService _customerService;

        [SetUp]
        public void Setup()
        {
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _customerService = new CustomerService(_customerRepositoryMock.Object);
        }

        [Test]
        public async Task CreateCustomerAsync_ShouldReturnCustomerResponseDto_WhenCustomerIsCreated()
        {
            // Arrange
            var createDto = new CreateCustomerDto
            {
                DocumentNumber = "123456789",
                FirstName = "John",
                LastName = "Doe",
                BirthDate = new DateOnly(1990, 1, 1),
                MonthlyIncome = 2000,
                Gender = "M"
            };

            var savedEntity = new Customer
            {
                CustomerId = 1,
                DocumentNumber = createDto.DocumentNumber,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                BirthDate = createDto.BirthDate,
                MonthlyIncome = createDto.MonthlyIncome,
                Gender = Enum.Parse<Gender>(createDto.Gender, ignoreCase: true)
            };

            _customerRepositoryMock
                .Setup(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(savedEntity);

            // Act
            var result = await _customerService.CreateCustomerAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.CustomerId, Is.EqualTo(savedEntity.CustomerId));
            Assert.That(result.DocumentNumber, Is.EqualTo(savedEntity.DocumentNumber));
            Assert.That(result.FirstName, Is.EqualTo(savedEntity.FirstName));
            Assert.That(result.LastName, Is.EqualTo(savedEntity.LastName));
            Assert.That(result.MonthlyIncome, Is.EqualTo(savedEntity.MonthlyIncome));
            Assert.That(result.Gender, Is.EqualTo(savedEntity.Gender));
        }

        [Test]
        public void CreateCustomerAsync_ShouldThrow_WhenRepositoryReturnsNull()
        {
            // Arrange
            var createDto = new CreateCustomerDto
            {
                DocumentNumber = "987654321",
                FirstName = "Jane",
                LastName = "Smith",
                BirthDate = new DateOnly(1985, 5, 15),
                MonthlyIncome = 3000,
                Gender = "F"
            };

            _customerRepositoryMock
                .Setup(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer)null!);

            // Act & Assert
            Assert.ThrowsAsync<NullReferenceException>(async () =>
                await _customerService.CreateCustomerAsync(createDto));
        }

        [Test]
        public async Task CreateCustomerAsync_ShouldCallRepositoryOnce()
        {
            // Arrange
            var createDto = new CreateCustomerDto
            {
                DocumentNumber = "55555555",
                FirstName = "Alice",
                LastName = "Johnson",
                BirthDate = new DateOnly(2000, 12, 12),
                MonthlyIncome = 1500,
                Gender = "F"
            };

            var entity = new Customer
            {
                CustomerId = 99,
                DocumentNumber = createDto.DocumentNumber,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                BirthDate = createDto.BirthDate,
                MonthlyIncome = createDto.MonthlyIncome,
                Gender = Enum.Parse<Gender>(createDto.Gender, ignoreCase: true)
            };

            _customerRepositoryMock
                .Setup(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            // Act
            var result = await _customerService.CreateCustomerAsync(createDto);

            // Assert
            _customerRepositoryMock.Verify(r => r.AddCustomerAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(result.FirstName, Is.EqualTo("Alice"));
            Assert.That(result.LastName, Is.EqualTo("Johnson"));
        }
    }
}
