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
    public class TransactionRepositoryTests
    {
        private BankingDbContext _context;
        private TransactionRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BankingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;

            _context = new BankingDbContext(options);
            _repository = new TransactionRepository(_context);
        }

        [TearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [Test]
        public async Task AddTransactionAsync_ShouldPersistTransaction()
        {
            var transaction = new Transaction
            {
                BankAccountId = 1,
                Amount = 100,
                TransactionDate = DateTime.UtcNow,
                TransactionType = Domain.Enums.TransactionType.Deposit,
                BalanceAfter = 200
            };

            var result = await _repository.AddTransactionAsync(transaction);

            Assert.NotNull(result);
            Assert.That(result.Amount, Is.EqualTo(100));
            Assert.That(await _context.Transactions.CountAsync(), Is.EqualTo(1 ));
        }

        [Test]
        public async Task GetAllByAccountIdAsync_ShouldReturnTransactionsOrdered()
        {
            _context.Transactions.AddRange(
                new Transaction { BankAccountId = 1, Amount = 100, TransactionDate = DateTime.UtcNow.AddDays(-1), TransactionType = Domain.Enums.TransactionType.Deposit },
                new Transaction { BankAccountId = 1, Amount = 50, TransactionDate = DateTime.UtcNow, TransactionType = Domain.Enums.TransactionType.Withdrawal }
            );
            await _context.SaveChangesAsync();

            var transactions = await _repository.GetAllByAccountIdAsync(1);

            Assert.That(transactions.Count(), Is.EqualTo(2));
            Assert.Less(transactions.First().TransactionDate, transactions.Last().TransactionDate);
        }
    }
}
