using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBankAccountService _bankAccountService;

        public TransactionService(ITransactionRepository transactionRepository, IBankAccountService bankAccountService)
        {
            _transactionRepository = transactionRepository;
            _bankAccountService = bankAccountService;
        }

        public async Task<TransactionResponseDto?> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            var account = await _bankAccountService.GetByAccountNumberAsync(accountNumber);
            if(account == null) 
                return null;

            var transaction = new Transaction
            {
                BankAccountId = account.BankAccountId,
                Amount = amount,
                TransactionType = TransactionType.Deposit,
                TransactionDate = DateTime.UtcNow,
                BalanceAfter = account.Balance + amount,
            };

            var result = await _transactionRepository.AddTransactionAsync(transaction, cancellationToken);

            account.Balance += amount;
            var resultAccount = await _bankAccountService.UpdateBankAccountAsync(account, cancellationToken);

            return new TransactionResponseDto 
                { 
                    AccountNumber = accountNumber, 
                    TransactionType = result.TransactionType, 
                    Amount = result.Amount ,
                    BalanceAfter = resultAccount.Balance,
            };

        }

        public async Task<TransactionResponseDto?> WithdrawAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            var account = await _bankAccountService.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                return null;

            if(account.Balance < amount)
                return null; // Insufficient funds

            var transaction = new Transaction
            {
                BankAccountId = account.BankAccountId,
                Amount = amount,
                TransactionType = TransactionType.Withdrawal,
                TransactionDate = DateTime.UtcNow,
                BalanceAfter = account.Balance - amount,
            };

            var result = await _transactionRepository.AddTransactionAsync(transaction, cancellationToken);

            account.Balance -= result.Amount;
            var resultAccount = await _bankAccountService.UpdateBankAccountAsync(account, cancellationToken);

            return new TransactionResponseDto
            {
                AccountNumber = accountNumber,
                TransactionType = result.TransactionType,
                Amount = result.Amount,
                BalanceAfter = resultAccount.Balance,
            };
        }

        public async Task<TransactionResponseDto?> ApplyInterestAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            var account = await _bankAccountService.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                return null;
            
            var transaction = new Transaction
            {
                BankAccountId = account.BankAccountId,
                Amount = account.Balance * account.InterestRate,
                TransactionType = TransactionType.Interest,
                TransactionDate = DateTime.UtcNow,
                BalanceAfter = account.Balance + (account.Balance * account.InterestRate) ,
            };

            var result = await _transactionRepository.AddTransactionAsync(transaction, cancellationToken);

            account.Balance += result.Amount;
            var resultAccount = await _bankAccountService.UpdateBankAccountAsync(account, cancellationToken);

            return new TransactionResponseDto
            {
                AccountNumber = accountNumber,
                TransactionType = result.TransactionType,
                Amount = result.Amount,
                BalanceAfter = resultAccount.Balance,
            };
        }

        public async Task<AccountTransactionSummaryDto?> GetAccountTransactionSummaryAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            var account = await _bankAccountService.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                return null;

            var transactions = await _transactionRepository.GetAllByAccountIdAsync(account.BankAccountId, cancellationToken);

            var transactionSummary = transactions.Select(t => new TransactionSummaryDto
            {
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                TransactionDate = t.TransactionDate,
            }).ToList();

            var totalDepositsAndInterest = transactionSummary
                .Where(t => t.TransactionType == TransactionType.Deposit || t.TransactionType == TransactionType.Interest)
                .Sum(t => t.Amount);

            var totalWithdrawals = transactionSummary
                .Where(t => t.TransactionType == TransactionType.Withdrawal)
                .Sum(t => t.Amount);

            return new AccountTransactionSummaryDto
            {
                AccountNumber = accountNumber,
                StartingBalance = account.Balance + totalWithdrawals - totalDepositsAndInterest,
                Transactions = transactionSummary,
                FinalBalance = account.Balance,
            };
        }
    }
}
