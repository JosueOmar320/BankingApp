using Banking.Application.Exceptions;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Rules;
using Banking.Tests.Support;

namespace Banking.Tests.Domain;

/// <summary>
/// Exceptions identify their cause with a stable code and expose the values the translated
/// message needs, so no layer has to parse the (English) message.
/// </summary>
public class ErrorCodeTests
{
    [Test]
    public void Withdraw_AboveBalance_ExposesInsufficientFundsCodeAndAmounts()
    {
        var account = TestData.Account(initialDeposit: 20m);

        var ex = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(20.01m, TestData.Now));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.InsufficientFunds));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { 20m, 20.01m }));
        });
    }

    [Test]
    public void Open_BelowMinimum_ExposesDepositBelowMinimumCodeAndMinimum()
    {
        var ex = Assert.Throws<DomainException>(() => BankAccount.Open("1234567890", 1, 9.99m, TestData.Now));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.DepositBelowMinimum));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { AccountRules.MinimumInitialDeposit }));
        });
    }

    [Test]
    public void Deposit_TooManyDecimals_ExposesTheAllowedScale()
    {
        var ex = Assert.Throws<DomainException>(() => TestData.Account().Deposit(1.005m, TestData.Now));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.AmountTooManyDecimals));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { AccountRules.MoneyScale }));
        });
    }

    [Test]
    public void Create_TooLongFirstName_ExposesTheMaximumLength()
    {
        var ex = Assert.Throws<DomainException>(() => Customer.Create(new string('a', Customer.MaxNameLength + 1), null, new DateOnly(1990, 1, 1), "DOC-1", Gender.F, 0m, TestData.Now));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.FirstNameTooLong));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { Customer.MaxNameLength }));
        });
    }

    [Test]
    public void Create_FutureBirthDate_ExposesBirthDateInFutureCode()
    {
        var ex = Assert.Throws<DomainException>(() => Customer.Create("Ana", null, new DateOnly(2999, 1, 1), "DOC-1", Gender.F, 0m, TestData.Now));

        Assert.That(ex!.Code, Is.EqualTo(DomainErrorCodes.BirthDateInFuture));
    }

    [Test]
    public void DomainException_WithoutCode_FallsBackToBusinessRuleViolated()
    {
        var ex = new DomainException("Anything.");

        Assert.Multiple(() =>
        {
            Assert.That(ex.Code, Is.EqualTo(DomainErrorCodes.BusinessRuleViolated));
            Assert.That(ex.MessageArguments, Is.Empty);
        });
    }

    [Test]
    public void DuplicateDocumentNumber_ForDocumentNumber_ExposesTheDocument()
    {
        var inner = new InvalidOperationException("unique index");

        var ex = DuplicateDocumentNumberException.ForDocumentNumber("DOC-9", inner);

        Assert.Multiple(() =>
        {
            Assert.That(ex.Code, Is.EqualTo(DomainErrorCodes.DuplicateDocumentNumber));
            Assert.That(ex.DocumentNumber, Is.EqualTo("DOC-9"));
            Assert.That(ex.MessageArguments, Is.EqualTo(new object[] { "DOC-9" }));
            Assert.That(ex.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void ConcurrencyConflict_Factories_UseDistinctCodes()
    {
        var inner = new InvalidOperationException("row version");

        var modified = ConcurrencyConflictException.ForAccount("1234567890", inner);
        var taken = ConcurrencyConflictException.AccountNumberTaken("1234567890", inner);

        Assert.Multiple(() =>
        {
            Assert.That(modified.Code, Is.EqualTo(DomainErrorCodes.ConcurrencyConflict));
            Assert.That(taken.Code, Is.EqualTo(DomainErrorCodes.AccountNumberTaken));
            Assert.That(modified.MessageArguments, Is.EqualTo(new object[] { "1234567890" }));
            Assert.That(new ConcurrencyConflictException().Code, Is.EqualTo(DomainErrorCodes.ConcurrencyConflict));
        });
    }

    [Test]
    public void NotFound_ByResource_DerivesTheCodeFromTheResource()
    {
        var customer = new NotFoundException("Customer", 42);
        var account = new NotFoundException("Account", "0000000000");

        Assert.Multiple(() =>
        {
            Assert.That(customer.Code, Is.EqualTo(ApplicationErrorCodes.CustomerNotFound));
            Assert.That(customer.MessageArguments, Is.EqualTo(new object[] { 42 }));
            Assert.That(account.Code, Is.EqualTo(ApplicationErrorCodes.AccountNotFound));
            Assert.That(new NotFoundException().Code, Is.EqualTo(NotFoundException.DefaultCode));
        });
    }
}
