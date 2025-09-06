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
    public class TransactionControllerTests
    {
        private Mock<ITransactionService> _mockService = null!;
        private TransactionController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _mockService = new Mock<ITransactionService>();
            _controller = new TransactionController(_mockService.Object);
        }

        [Test]
        public async Task Deposit_ShouldReturnOk_WhenSuccessful()
        {
            var dto = new TransactionResponseDto
            {
                AccountNumber = "123",
                Amount = 100,
                TransactionType = TransactionType.Deposit,
                BalanceAfter = 200
            };

            _mockService.Setup(s => s.DepositAsync("123", 100, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(dto);

            var result = await _controller.Deposit("123", 100);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task Deposit_ShouldReturnBadRequest_WhenAmountIsZero()
        {
            var result = await _controller.Deposit("123", 0);
            var badRequest = result as BadRequestObjectResult;
            Assert.NotNull(badRequest);
        }

        [Test]
        public async Task Deposit_ShouldReturnBadRequest_WhenAmountIsNegative()
        {
            var result = await _controller.Deposit("123", -2);
            var badRequest = result as BadRequestObjectResult;
            Assert.NotNull(badRequest);
        }

        [Test]
        public async Task Deposit_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            _mockService.Setup(s => s.DepositAsync("999", 100, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((TransactionResponseDto?)null);

            var result = await _controller.Deposit("999", 100);

            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task Withdrawal_ShouldReturnOk_WhenSuccessful()
        {
            var dto = new TransactionResponseDto
            {
                AccountNumber = "123",
                Amount = 50,
                TransactionType = TransactionType.Withdrawal,
                BalanceAfter = 150
            };

            _mockService.Setup(s => s.WithdrawAsync("123", 50, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(dto);

            var result = await _controller.Withdrawal("123", 50);

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Withdrawal_ShouldReturnBadRequest_WhenAmountIsZero()
        {
            var result = await _controller.Withdrawal("123", 0);
            var badRequest = result as BadRequestObjectResult;
            Assert.NotNull(badRequest);
        }

        [Test]
        public async Task Withdrawal_ShouldReturnBadRequest_WhenAmountIsNegative()
        {
            var result = await _controller.Withdrawal("123", -2);
            var badRequest = result as BadRequestObjectResult;
            Assert.NotNull(badRequest);
        }

        [Test]
        public async Task ApplyInterest_ShouldReturnOk_WhenSuccessful()
        {
            var dto = new TransactionResponseDto
            {
                AccountNumber = "123",
                Amount = 20,
                TransactionType = TransactionType.Interest,
                BalanceAfter = 220
            };

            _mockService.Setup(s => s.ApplyInterestAsync("123", It.IsAny<CancellationToken>()))
                        .ReturnsAsync(dto);

            var result = await _controller.ApplyInterest("123");

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task GetAccountTransactionSummary_ShouldReturnNotFound_WhenAccountNotExist()
        {
            _mockService.Setup(s => s.GetAccountTransactionSummaryAsync("999", It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AccountTransactionSummaryDto?)null);

            var result = await _controller.GetAccountTransactionSummary("999");

            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }
    }
}
