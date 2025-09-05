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
        private readonly IBankAccountRepository _accountRepository;

        public TransactionService(ITransactionRepository transactionRepository, IBankAccountRepository accountRepository)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
        }

        public async Task<TransactionResponseDto?> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if(account == null) 
                return null;

            var transaction = new Transaction
            {
                BankAccountId = account.BankAccountId,
                Amount = amount,
                TransactionType = TransactionType.Deposit,
                TransactionDate = DateTime.UtcNow,
            };

            var result = await _transactionRepository.AddAsync(transaction, cancellationToken);

            account.Balance += amount;
            var resultAccount = await _accountRepository.UpdateBankAccountAsync(account, cancellationToken);

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
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                return null;

            if(account.Balance < amount)
                return null; // Insufficient funds

            var transaction = new Transaction
            {
                BankAccountId = account.BankAccountId,
                Amount = amount,
                TransactionType = TransactionType.Withdraw,
                TransactionDate = DateTime.UtcNow,
            };

            var result = await _transactionRepository.AddAsync(transaction, cancellationToken);

            account.Balance -= result.Amount;
            var resultAccount = await _accountRepository.UpdateBankAccountAsync(account, cancellationToken);

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
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                return null;
            
            var transaction = new Transaction
            {
                BankAccountId = account.BankAccountId,
                Amount = account.Balance * account.InterestRate,
                TransactionType = TransactionType.Interest,
                TransactionDate = DateTime.UtcNow,
            };

            var result = await _transactionRepository.AddAsync(transaction, cancellationToken);

            account.Balance += result.Amount;
            var resultAccount = await _accountRepository.UpdateBankAccountAsync(account, cancellationToken);

            return new TransactionResponseDto
            {
                AccountNumber = accountNumber,
                TransactionType = result.TransactionType,
                Amount = result.Amount,
                BalanceAfter = resultAccount.Balance,
            };
        }
    }
}
