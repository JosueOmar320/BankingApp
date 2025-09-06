using Banking.Domain.Entities;
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
    public class BankAccountRepositoryTests
    {
        private BankingDbContext _context;
        private BankAccountRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BankingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new BankingDbContext(options);
            _repository = new BankAccountRepository(_context);
        }

        [TearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [Test]
        public async Task AddBankAccountAsync_ShouldPersistAccount()
        {
            var account = new BankAccount { AccountNumber = "123", Balance = 500, CustomerId = 1 };

            var result = await _repository.AddBankAccountAsync(account);

            Assert.NotNull(result);
            Assert.That(result.AccountNumber, Is.EqualTo("123"));
            Assert.That(await _context.BankAccounts.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetBalanceByAccountNumberAsync_ShouldReturnBalance()
        {
            _context.BankAccounts.Add(new BankAccount { AccountNumber = "123", Balance = 1000, CustomerId = 1 });
            await _context.SaveChangesAsync();

            var balance = await _repository.GetBalanceByAccountNumberAsync("123");

            Assert.That(balance, Is.EqualTo(1000));
        }

        [Test]
        public async Task GetByAccountNumberAsync_ShouldReturnAccount()
        {
            var account = new BankAccount { AccountNumber = "456", Balance = 200, CustomerId = 2 };
            _context.BankAccounts.Add(account);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByAccountNumberAsync("456");

            Assert.NotNull(result);
            Assert.That(result.AccountNumber, Is.EqualTo("456"));
        }

        [Test]
        public async Task UpdateBankAccountAsync_ShouldUpdateBalance()
        {
            var account = new BankAccount { AccountNumber = "789", Balance = 300, CustomerId = 3 };
            _context.BankAccounts.Add(account);
            await _context.SaveChangesAsync();

            account.Balance = 600;
            var result = await _repository.UpdateBankAccountAsync(account);

            Assert.That(result.Balance, Is.EqualTo(600));
        }

        [Test]
        public async Task ExistsByNumberAsync_ShouldReturnTrue_WhenAccountExists()
        {
            _context.BankAccounts.Add(new BankAccount { AccountNumber = "111", Balance = 400, CustomerId = 4 });
            await _context.SaveChangesAsync();

            var exists = await _repository.ExistsByNumberAsync("111", default);

            Assert.IsTrue(exists);
        }

        [Test]
        public async Task ExistsByNumberAsync_ShouldReturnFalse_WhenAccountDoesNotExist()
        {
            var exists = await _repository.ExistsByNumberAsync("999", default);

            Assert.IsFalse(exists);
        }
    }
}
