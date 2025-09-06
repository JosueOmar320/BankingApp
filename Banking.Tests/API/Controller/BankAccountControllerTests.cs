using Banking.API.Controllers;
using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Tests.API.Controller
{
    public class BankAccountControllerTests
    {
        private Mock<IBankAccountService> _mockService = null!;
        private BankAccountController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<IBankAccountService>();
            _controller = new BankAccountController(_mockService.Object);
        }

        [Test]
        public async Task CreateBankAccount_ShouldReturnCreated()
        {
            var dto = new BankAccountResponseDto { AccountNumber = "123" };
            _mockService.Setup(s => s.CreateBankAccountAsync(It.IsAny<CreateBankAccountDto>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(dto);

            var result = await _controller.CreateBankAccount(new CreateBankAccountDto());

            Assert.IsInstanceOf<CreatedResult>(result);
        }

        [Test]
        public async Task GetBalanceByAccountNumber_ShouldReturnNotFound_WhenAccountNotExist()
        {
            _mockService.Setup(s => s.GetBalanceByAccountNumberAsync("999", It.IsAny<CancellationToken>()))
                        .ReturnsAsync((decimal?)null);

            var result = await _controller.GetBalanceByAccountNumber("999");

            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task GetBalanceByAccountNumber_ShouldReturnOk_WhenAccountExists()
        {
            _mockService.Setup(s => s.GetBalanceByAccountNumberAsync("123", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(500);

            var result = await _controller.GetBalanceByAccountNumber("123");

            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}
