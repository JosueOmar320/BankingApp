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

        // Accounts loaded through this repository are already tracked, so EF knows the original
        // Version and the new transactions. Attach untracked instances the same way.
        if (_context.Entry(account).State == EntityState.Detached)
        {
            _context.BankAccounts.Update(account);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw ConcurrencyConflictException.ForAccount(account.AccountNumber, ex);
        }

        return account;
    }
}
