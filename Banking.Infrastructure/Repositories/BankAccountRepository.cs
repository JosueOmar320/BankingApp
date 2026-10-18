using Banking.Domain.Entities;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;
using Banking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Repositories;

/// <inheritdoc cref="IBankAccountRepository"/>
public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly BankingDbContext _context;

    public BankAccountRepository(BankingDbContext context)
    {
        _context = context;
    }

    public Task<BankAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        => _context.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);

    public Task<bool> ExistsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        => _context.BankAccounts.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);

    public async Task<BankAccount> AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        _context.BankAccounts.Add(account);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqliteErrors.IsUniqueConstraintViolation(ex))
        {
            _context.Entry(account).State = EntityState.Detached;
            throw ConcurrencyConflictException.AccountNumberTaken(account.AccountNumber, ex);
        }

        return account;
    }

    public async Task<BankAccount> UpdateBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        Track(account);
        await SaveTrackedAsync(account, cancellationToken);

        return account;
    }

    public async Task SaveTransferAsync(BankAccount source, BankAccount destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        // One SaveChanges, one transaction: both balances and both ledger entries, or nothing.
        Track(source);
        Track(destination);
        await SaveTrackedAsync(source, cancellationToken);
    }

    /// <summary>
    /// Accounts loaded through this repository are already tracked, so EF knows the original
    /// Version and the new transactions. Untracked instances are attached the same way.
    /// </summary>
    private void Track(BankAccount account)
    {
        if (_context.Entry(account).State == EntityState.Detached)
        {
            _context.BankAccounts.Update(account);
        }
    }

    private async Task SaveTrackedAsync(BankAccount fallback, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Name the account whose version no longer matched; EF reports it in the failed entries.
            var conflicting = ex.Entries.Select(entry => entry.Entity).OfType<BankAccount>().FirstOrDefault() ?? fallback;
            throw ConcurrencyConflictException.ForAccount(conflicting.AccountNumber, ex);
        }
    }
}
