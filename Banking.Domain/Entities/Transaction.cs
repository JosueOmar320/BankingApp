using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

/// <summary>
/// An immutable ledger entry. Transactions are only created by <see cref="BankAccount"/>
/// so the balance and the ledger can never disagree.
/// </summary>
public class Transaction
{
    // EF Core materialisation.
    private Transaction()
    {
    }

    internal Transaction(TransactionType transactionType, decimal amount, decimal balanceAfter, DateTime transactionDate)
    {
        TransactionType = transactionType;
        Amount = amount;
        BalanceAfter = balanceAfter;
        TransactionDate = transactionDate;
    }

    public int TransactionId { get; private set; }

    /// <summary>Owning account. Assigned by EF Core when the account is saved.</summary>
    public int BankAccountId { get; private set; }

    public TransactionType TransactionType { get; private set; }

    /// <summary>Always positive; <see cref="TransactionType"/> gives the direction.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Account balance right after this transaction was applied.</summary>
    public decimal BalanceAfter { get; private set; }

    /// <summary>UTC timestamp of the movement.</summary>
    public DateTime TransactionDate { get; private set; }
}
