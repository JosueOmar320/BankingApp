using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Rules;

namespace Banking.Domain.Entities;

/// <summary>
/// Aggregate root for an account and its ledger. Every balance change goes through one of the
/// operations below, which record the matching <see cref="Transaction"/> and bump the
/// concurrency <see cref="Version"/>, so the balance and the ledger are always saved together.
/// </summary>
public class BankAccount
{
    private readonly List<Transaction> _transactions = [];

    // EF Core materialisation.
    private BankAccount()
    {
    }

    public int BankAccountId { get; private set; }

    public string AccountNumber { get; private set; } = null!;

    public int CustomerId { get; private set; }

    public decimal Balance { get; private set; }

    /// <summary>Interest rate applied by <see cref="ApplyInterest"/> (e.g. 0.10 for 10%).</summary>
    public decimal InterestRate { get; private set; }

    /// <summary>UTC timestamp of account opening.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Optimistic-concurrency token; incremented on every balance change.</summary>
    public int Version { get; private set; }

    /// <summary>Ledger entries added in this unit of work (plus any explicitly loaded ones).</summary>
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    /// <summary>Opens an account and records the opening deposit as its first transaction.</summary>
    /// <exception cref="DomainException">The deposit is below the minimum or malformed.</exception>
    public static BankAccount Open(string accountNumber, int customerId, decimal initialDeposit, DateTime openedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            throw new DomainException("Account number is required.");
        }

        if (customerId <= 0)
        {
            throw new DomainException("A valid customer is required to open an account.");
        }

        EnsureValidAmount(initialDeposit);
        if (initialDeposit < AccountRules.MinimumInitialDeposit)
        {
            throw new DomainException($"The initial deposit must be at least {AccountRules.MinimumInitialDeposit:0.00}.");
        }

        var account = new BankAccount
        {
            AccountNumber = accountNumber.Trim(),
            CustomerId = customerId,
            InterestRate = AccountRules.InterestRate,
            CreatedAt = openedAtUtc,
        };

        account.Post(TransactionType.Deposit, initialDeposit, openedAtUtc);
        return account;
    }

    /// <summary>Adds money to the account.</summary>
    /// <exception cref="DomainException">The amount is not positive or has more than two decimals.</exception>
    public Transaction Deposit(decimal amount, DateTime atUtc)
    {
        EnsureValidAmount(amount);
        return Post(TransactionType.Deposit, amount, atUtc);
    }

    /// <summary>Takes money out of the account.</summary>
    /// <exception cref="DomainException">The amount is not positive or has more than two decimals.</exception>
    /// <exception cref="InsufficientFundsException">The amount exceeds the balance.</exception>
    public Transaction Withdraw(decimal amount, DateTime atUtc)
    {
        EnsureValidAmount(amount);
        if (amount > Balance)
        {
            throw new InsufficientFundsException(Balance, amount);
        }

        return Post(TransactionType.Withdrawal, amount, atUtc);
    }

    /// <summary>Credits <see cref="InterestRate"/> of the current balance, rounded to cents.</summary>
    /// <exception cref="DomainException">The resulting interest rounds to zero.</exception>
    public Transaction ApplyInterest(DateTime atUtc)
    {
        var interest = Math.Round(Balance * InterestRate, AccountRules.MoneyScale, MidpointRounding.ToEven);
        if (interest <= 0)
        {
            throw new DomainException("Interest cannot be applied because the resulting amount is zero.");
        }

        return Post(TransactionType.Interest, interest, atUtc);
    }

    private Transaction Post(TransactionType type, decimal amount, DateTime atUtc)
    {
        Balance = type == TransactionType.Withdrawal ? Balance - amount : Balance + amount;
        Version++;

        var transaction = new Transaction(type, amount, Balance, atUtc);
        _transactions.Add(transaction);
        return transaction;
    }

    private static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("The amount must be greater than zero.");
        }

        if (decimal.Round(amount, AccountRules.MoneyScale) != amount)
        {
            throw new DomainException($"The amount cannot have more than {AccountRules.MoneyScale} decimal places.");
        }
    }
}
