using Banking.Application.Interfaces;
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
    public class TransactionServiceTests
    {
        private Mock<ITransactionRepository> _transactionRepoMock;
        private Mock<IBankAccountService> _bankAccountServiceMock;
        private TransactionService _transactionService;

        [SetUp]
        public void Setup()
        {
            _transactionRepoMock = new Mock<ITransactionRepository>();
            _bankAccountServiceMock = new Mock<IBankAccountService>();
            _transactionService = new TransactionService(_transactionRepoMock.Object, _bankAccountServiceMock.Object);
        }

        [Test]
        public async Task DepositAsync_ShouldIncreaseBalance()
        {
            // Arrange
            var account = new BankAccount { BankAccountId = 1, AccountNumber = "123", Balance = 100 };
            _bankAccountServiceMock.Setup(s => s.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(account);
            _transactionRepoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync((Transaction t, CancellationToken _) => t);
            _bankAccountServiceMock.Setup(s => s.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync((BankAccount acc, CancellationToken _) => acc);

            // Act
            var result = await _transactionService.DepositAsync("123", 50);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(result.BalanceAfter, Is.EqualTo(150));
        }

        [Test]
        public void DepositAsync_ShouldThrow_WhenAmountIsZero()
        {
            var account = new BankAccount { BankAccountId = 1, AccountNumber = "123", Balance = 100 };
            _bankAccountServiceMock.Setup(s => s.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(account);

            Assert.ThrowsAsync<ArgumentException>(() => _transactionService.DepositAsync("123", 0));
        }

        [Test]
        public async Task WithdrawAsync_ShouldDecreaseBalance()
        {
            // Arrange
            var account = new BankAccount { BankAccountId = 1, AccountNumber = "123", Balance = 200 };
            _bankAccountServiceMock.Setup(s => s.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(account);
            _transactionRepoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync((Transaction t, CancellationToken _) => t);
            _bankAccountServiceMock.Setup(s => s.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync((BankAccount acc, CancellationToken _) => acc);

            // Act
            var result = await _transactionService.WithdrawAsync("123", 50);

            // Assert
            Assert.NotNull(result);
            Assert.That(result.TransactionType, Is.EqualTo(TransactionType.Withdrawal));
            Assert.That(result.BalanceAfter, Is.EqualTo(150));
        }

        [Test]
        public void WithdrawAsync_ShouldThrow_WhenInsufficientFunds()
        {
            var account = new BankAccount { BankAccountId = 1, AccountNumber = "123", Balance = 20 };
            _bankAccountServiceMock.Setup(s => s.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(account);

            Assert.ThrowsAsync<InvalidOperationException>(() => _transactionService.WithdrawAsync("123", 50));
        }

        [Test]
        public async Task ApplyInterestAsync_ShouldIncreaseBalanceByRate()
        {
            // Arrange
            var account = new BankAccount { BankAccountId = 1, AccountNumber = "123", Balance = 100, InterestRate = 0.1m };
            _bankAccountServiceMock.Setup(s => s.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(account);
            _transactionRepoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync((Transaction t, CancellationToken _) => t);
            _bankAccountServiceMock.Setup(s => s.UpdateBankAccountAsync(It.IsAny<BankAccount>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync((BankAccount acc, CancellationToken _) => acc);

            // Act
            var result = await _transactionService.ApplyInterestAsync("123");

            // Assert
            Assert.NotNull(result);
            Assert.That(result.TransactionType, Is.EqualTo(TransactionType.Interest));
            Assert.That(result.BalanceAfter, Is.EqualTo(110));
        }

        [Test]
        public async Task GetAccountTransactionSummaryAsync_ShouldReturnSummary()
        {
            // Arrange
            var account = new BankAccount { BankAccountId = 1, AccountNumber = "123", Balance = 150 };
            var transactions = new List<Transaction>
            {
                new Transaction { TransactionType = TransactionType.Deposit, Amount = 100, BalanceAfter = 100, TransactionDate = DateTime.UtcNow },
                new Transaction { TransactionType = TransactionType.Withdrawal, Amount = 50, BalanceAfter = 50, TransactionDate = DateTime.UtcNow }
            };

            _bankAccountServiceMock.Setup(s => s.GetByAccountNumberAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(account);
            _transactionRepoMock.Setup(r => r.GetAllByAccountIdAsync(account.BankAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);

            // Act
            var result = await _transactionService.GetAccountTransactionSummaryAsync("123");

            // Assert
            Assert.NotNull(result);
            Assert.That(result.AccountNumber, Is.EqualTo("123"));
            Assert.That(result.Transactions.Count, Is.EqualTo(2));
            Assert.That(result.FinalBalance, Is.EqualTo(account.Balance));
        }
    }
}
