using Banking.Domain.Enums;

namespace Banking.Domain.ValueObjects;

/// <summary>
/// Amount moved by each kind of transaction over an account's whole ledger, computed by the
/// database so a summary never has to load every transaction.
/// </summary>
public sealed record LedgerTotals
{
    private readonly IReadOnlyDictionary<TransactionType, decimal> _amountByType;

    /// <param name="transactionCount">Number of transactions in the ledger.</param>
    /// <param name="amountByType">Sum of the amounts of each transaction type; types without transactions may be absent.</param>
    public LedgerTotals(int transactionCount, IReadOnlyDictionary<TransactionType, decimal> amountByType)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(transactionCount);
        ArgumentNullException.ThrowIfNull(amountByType);

        TransactionCount = transactionCount;
        _amountByType = amountByType;
    }

    /// <summary>Totals of an account without transactions.</summary>
    public static LedgerTotals Empty { get; } = new(0, new Dictionary<TransactionType, decimal>());

    /// <summary>Number of transactions in the ledger.</summary>
    public int TransactionCount { get; }

    /// <summary>Sum of the amounts of one transaction type; zero when there are none.</summary>
    public decimal Of(TransactionType type) => _amountByType.TryGetValue(type, out var amount) ? amount : 0m;
}
