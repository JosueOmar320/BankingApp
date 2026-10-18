namespace Banking.Application.Dtos;

/// <summary>Result of a transfer, seen from the source account.</summary>
/// <param name="TransactionId">Identifier of the <c>TransferOut</c> ledger entry on the source account.</param>
/// <param name="SourceAccountNumber">Account the money left.</param>
/// <param name="DestinationAccountNumber">Account that received the money.</param>
/// <param name="Amount">Amount moved.</param>
/// <param name="SourceBalanceAfter">Balance of the source account right after the transfer.</param>
/// <param name="TransactionDate">UTC timestamp of the transfer (the same on both ledgers).</param>
public sealed record TransferResponseDto(
    int TransactionId,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    decimal Amount,
    decimal SourceBalanceAfter,
    DateTime TransactionDate);
