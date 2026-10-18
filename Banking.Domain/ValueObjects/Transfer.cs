using Banking.Domain.Entities;

namespace Banking.Domain.ValueObjects;

/// <summary>The two ledger entries a transfer records: the debit on the source account and the credit on the destination.</summary>
/// <param name="Debit">The <see cref="Enums.TransactionType.TransferOut"/> entry on the source account.</param>
/// <param name="Credit">The <see cref="Enums.TransactionType.TransferIn"/> entry on the destination account.</param>
public sealed record Transfer(Transaction Debit, Transaction Credit);
