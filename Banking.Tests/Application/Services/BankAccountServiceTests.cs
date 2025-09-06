using Banking.Application.Dtos;
using Banking.Application.Services;
using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Tests.Application.Services
{
    public class BankAccountServiceTests
    {
        private Mock<IBankAccountRepository> _bankAccountRepositoryMock;
        private BankAccountService _bankAccountService;

        [SetUp]
        public void Setup()
        {
            _bankAccountRepositoryMock = new Mock<IBankAccountRepository>();
            _bankAccountService = new BankAccountService(_bankAccountRepositoryMock.Object);
        }

        [Test]
        public async Task CreateBankAccountAsync_ShouldReturnResponseDto_WhenAccountCreated()
        {
            // Arrange
            var dto = new CreateBankAccountDto { CustomerId = 1, InitialDeposit = 1000 };
            var fakeAccount = new BankAccount { BankAccountId = 1, AccountNumber = "1234567890", Balance = 1000, CustomerId = 1 };

            _bankAccountRepositoryMock
                .Setup(r => r.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeAccount);

            _bankAccountRepositoryMock
                .Setup(r => r.ExistsByNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _bankAccountService.CreateBankAccountAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.AccountNumber, Is.EqualTo(fakeAccount.AccountNumber));
            _bankAccountRepositoryMock.Verify(r => r.AddBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetBalanceByAccountNumberAsync_ShouldReturnBalance()
        {
            // Arrange
            _bankAccountRepositoryMock
                .Setup(r => r.GetBalanceByAccountNumberAsync("123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(500);

            // Act
            var balance = await _bankAccountService.GetBalanceByAccountNumberAsync("123");

            // Assert
            Assert.That(balance, Is.EqualTo(500));
        }

        [Test]
        public async Task GetByAccountNumberAsync_ShouldReturnAccount_WhenExists()
        {
            // Arrange
            var account = new BankAccount { AccountNumber = "123", Balance = 200 };
            _bankAccountRepositoryMock
                .Setup(r => r.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            // Act
            var result = await _bankAccountService.GetByAccountNumberAsync("123");

            // Assert
            Assert.NotNull(result);
            Assert.That(result.AccountNumber, Is.EqualTo("123"));
        }
    }
}
