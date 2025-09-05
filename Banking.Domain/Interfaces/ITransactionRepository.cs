using Banking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction> AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);

        Task<IEnumerable<Transaction>> GetAllByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default);
    }
}
