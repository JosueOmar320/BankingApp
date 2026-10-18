using Banking.Domain.Entities;
using Banking.Domain.Enums;
using CustomerEntity = Banking.Domain.Entities.Customer;

namespace Banking.Tests.Support;

internal static class TestData
{
    public static readonly DateTime Now = FixedTimeProvider.DefaultNow.UtcDateTime;

    public static Customer Customer(string documentNumber = "001-010190-0001A", string firstName = "Ana")
        => CustomerEntity.Create(firstName, "Pérez", new DateOnly(1990, 1, 1), documentNumber, Gender.F, 1500m, Now);

    public static BankAccount Account(int customerId = 1, decimal initialDeposit = 100m, string accountNumber = "1234567890")
        => BankAccount.Open(accountNumber, customerId, initialDeposit, Now);
}
