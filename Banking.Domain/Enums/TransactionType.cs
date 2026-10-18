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
}
