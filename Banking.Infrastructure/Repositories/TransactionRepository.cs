using Banking.Domain.Entities;
using Banking.Domain.Interfaces;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

/// <inheritdoc cref="ITransactionRepository"/>
public sealed class TransactionRepository : ITransactionRepository
{
    private readonly BankingDbContext _context;

    public TransactionRepository(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Transaction>> GetAllByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default)
        => await _context.Transactions
            .AsNoTracking()
            .Where(t => t.BankAccountId == bankAccountId)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.TransactionId)
            .ToListAsync(cancellationToken);
}
