using Banking.Application.Dtos;
using Banking.Application.Interfaces;
using Banking.Domain.Entities;
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
        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }
        public Task<TransactionResponseDto> DepositAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();

        }

        public Task<TransactionResponseDto> WithdrawAsync(string accountNumber, decimal amount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
