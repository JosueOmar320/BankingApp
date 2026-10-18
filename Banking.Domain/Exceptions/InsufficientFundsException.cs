namespace Banking.Domain.Exceptions;

/// <summary>Raised when a withdrawal exceeds the available balance.</summary>
public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException()
        : base("Insufficient funds.")
    {
    }

    public InsufficientFundsException(string message)
        : base(message)
    {
    }

    public InsufficientFundsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InsufficientFundsException(decimal balance, decimal requested)
        : base($"Insufficient funds: the available balance is {balance:0.00} and {requested:0.00} was requested.")
    {
        Balance = balance;
        Requested = requested;
    }

    public decimal Balance { get; }

    public decimal Requested { get; }
}
