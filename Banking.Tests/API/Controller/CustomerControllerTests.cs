using Banking.API.Controllers;
using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Banking.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Tests.API.Controller
{
    public class CustomerControllerTests
    {
        private Mock<ICustomerService> _mockService = null!;
        private CustomerController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<ICustomerService>();
            _controller = new CustomerController(_mockService.Object);
        }

        [Test]
        public async Task CreateCustomer_ShouldReturnCreated()
        {
            var dto = new CustomerResponseDto { CustomerId = 1, FirstName = "Juan", DocumentNumber = "001", Gender = Gender.M };
            _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(dto);

            var result = await _controller.CreateCustomer(new CreateCustomerDto { FirstName = "Juan", DocumentNumber = "001", Gender = "M" });

            Assert.IsInstanceOf<CreatedResult>(result);
        }
    }
}
