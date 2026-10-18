using Banking.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Banking.Application.Services;

/// <summary>Source-generated, allocation-free log messages for the application services.</summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Customer {CustomerId} registered")]
    public static partial void CustomerRegistered(ILogger logger, int customerId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Account {BankAccountId} opened for customer {CustomerId}")]
    public static partial void AccountOpened(ILogger logger, int bankAccountId, int customerId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "{TransactionType} of {Amount} applied to account {BankAccountId}; balance is now {Balance}")]
    public static partial void TransactionApplied(ILogger logger, TransactionType transactionType, decimal amount, int bankAccountId, decimal balance);
}
