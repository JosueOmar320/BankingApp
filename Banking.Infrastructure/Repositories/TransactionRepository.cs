using Banking.Domain.Enums;
using Banking.Domain.Interfaces;
using Banking.Domain.Rules;
using Banking.Domain.ValueObjects;
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

    public async Task<LedgerPage> GetPageByAccountIdAsync(int bankAccountId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var ledger = _context.Transactions
            .AsNoTracking()
            .Where(t => t.BankAccountId == bankAccountId);

        var totalCount = await ledger.CountAsync(cancellationToken);
        var transactions = await ledger
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.TransactionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new LedgerPage(transactions, page, pageSize, totalCount);
    }

    public async Task<LedgerTotals> GetTotalsByAccountIdAsync(int bankAccountId, CancellationToken cancellationToken = default)
    {
        // SQLite stores decimals as text and cannot SUM them, so the sum runs over REAL values.
        // Amounts have at most two decimals, which a double adds exactly for any realistic ledger
        // (up to about 70 billion), and the result is rounded back to the money scale.
        var rows = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.BankAccountId == bankAccountId)
            .GroupBy(t => t.TransactionType)
            .Select(group => new { Type = group.Key, Count = group.Count(), Amount = group.Sum(t => (double)t.Amount) })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return LedgerTotals.Empty;
        }

        var amountByType = rows.ToDictionary(
            row => row.Type,
            row => decimal.Round((decimal)row.Amount, AccountRules.MoneyScale, MidpointRounding.AwayFromZero));

        return new LedgerTotals(rows.Sum(row => row.Count), amountByType);
    }
}
