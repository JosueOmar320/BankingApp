using Banking.Application.Dtos;
using Banking.Domain.Entities;

namespace Banking.Application.Mapping;

/// <summary>Entity to DTO projections, kept in one place so every endpoint returns the same shape.</summary>
internal static class DtoMapping
{
    public static CustomerResponseDto ToDto(this Customer customer) => new(
        customer.CustomerId,
        customer.FirstName,
        customer.LastName,
        customer.BirthDate,
        customer.DocumentNumber,
        customer.Gender,
        customer.MonthlyIncome,
        customer.CreatedAt);

    public static BankAccountResponseDto ToDto(this BankAccount account) => new(
        account.AccountNumber,
        account.CustomerId,
        account.Balance,
        account.InterestRate,
        account.CreatedAt);

    public static TransactionResponseDto ToDto(this Transaction transaction, string accountNumber) => new(
        transaction.TransactionId,
        accountNumber,
        transaction.TransactionType,
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.TransactionDate);

    public static TransactionSummaryDto ToSummaryDto(this Transaction transaction) => new(
        transaction.TransactionId,
        transaction.TransactionType,
        transaction.Amount,
        transaction.BalanceAfter,
        transaction.TransactionDate);
}
