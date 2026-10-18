namespace Banking.Domain.Enums;

/// <summary>Kind of movement recorded against a bank account.</summary>
public enum TransactionType
{
    /// <summary>Money added to the account (includes the opening deposit).</summary>
    Deposit,

    /// <summary>Money taken out of the account.</summary>
    Withdrawal,

    /// <summary>Interest credited on the balance.</summary>
    Interest,

    /// <summary>Money sent to another account.</summary>
    TransferOut,

    /// <summary>Money received from another account.</summary>
    TransferIn,
}

/// <summary>Direction of each <see cref="TransactionType"/>.</summary>
public static class TransactionTypeExtensions
{
    /// <summary>Whether the type takes money out of the account (<see langword="false"/> means it adds money).</summary>
    public static bool IsDebit(this TransactionType type) => type is TransactionType.Withdrawal or TransactionType.TransferOut;
}
