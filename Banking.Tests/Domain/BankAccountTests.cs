using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Rules;
using Banking.Tests.Support;

namespace Banking.Tests.Domain;

public class BankAccountTests
{
    private static readonly DateTime Now = TestData.Now;

    [Test]
    public void Open_RecordsOpeningDepositAsFirstTransaction()
    {
        var account = BankAccount.Open("1234567890", customerId: 7, initialDeposit: 50m, Now);

        Assert.Multiple(() =>
        {
            Assert.That(account.AccountNumber, Is.EqualTo("1234567890"));
            Assert.That(account.CustomerId, Is.EqualTo(7));
            Assert.That(account.Balance, Is.EqualTo(50m));
            Assert.That(account.InterestRate, Is.EqualTo(AccountRules.InterestRate));
            Assert.That(account.CreatedAt, Is.EqualTo(Now));
            Assert.That(account.Version, Is.EqualTo(1));
            Assert.That(account.Transactions, Has.Count.EqualTo(1));
        });

        var opening = account.Transactions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(opening.TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(opening.Amount, Is.EqualTo(50m));
            Assert.That(opening.BalanceAfter, Is.EqualTo(50m));
            Assert.That(opening.TransactionDate, Is.EqualTo(Now));
        });
    }

    [TestCase(9.99)]
    [TestCase(0)]
    [TestCase(-10)]
    public void Open_RejectsDepositBelowMinimum(decimal initialDeposit)
    {
        var ex = Assert.Throws<DomainException>(() => BankAccount.Open("1234567890", 1, initialDeposit, Now));
        Assert.That(ex!.Message, Does.Contain("at least").Or.Contain("greater than zero"));
    }

    [Test]
    public void Open_AcceptsExactlyTheMinimumDeposit()
    {
        var account = BankAccount.Open("1234567890", 1, AccountRules.MinimumInitialDeposit, Now);
        Assert.That(account.Balance, Is.EqualTo(AccountRules.MinimumInitialDeposit));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Open_RequiresAccountNumber(string accountNumber)
        => Assert.Throws<DomainException>(() => BankAccount.Open(accountNumber, 1, 100m, Now));

    [Test]
    public void Open_RequiresCustomer()
        => Assert.Throws<DomainException>(() => BankAccount.Open("1234567890", 0, 100m, Now));

    [Test]
    public void Deposit_IncreasesBalanceAndRecordsTransaction()
    {
        var account = TestData.Account(initialDeposit: 100m);
        var later = Now.AddMinutes(5);

        var transaction = account.Deposit(25.50m, later);

        Assert.Multiple(() =>
        {
            Assert.That(account.Balance, Is.EqualTo(125.50m));
            Assert.That(account.Version, Is.EqualTo(2));
            Assert.That(account.Transactions, Has.Count.EqualTo(2));
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionType.Deposit));
            Assert.That(transaction.Amount, Is.EqualTo(25.50m));
            Assert.That(transaction.BalanceAfter, Is.EqualTo(125.50m));
            Assert.That(transaction.TransactionDate, Is.EqualTo(later));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Deposit_RejectsNonPositiveAmount(decimal amount)
    {
        var account = TestData.Account();
        Assert.Throws<DomainException>(() => account.Deposit(amount, Now));
        Assert.That(account.Balance, Is.EqualTo(100m), "balance must not change on failure");
    }

    [Test]
    public void Deposit_RejectsMoreThanTwoDecimals()
    {
        var account = TestData.Account();
        var ex = Assert.Throws<DomainException>(() => account.Deposit(10.005m, Now));
        Assert.That(ex!.Message, Does.Contain("decimal"));
    }

    [Test]
    public void Withdraw_DecreasesBalanceAndRecordsTransaction()
    {
        var account = TestData.Account(initialDeposit: 100m);

        var transaction = account.Withdraw(40m, Now);

        Assert.Multiple(() =>
        {
            Assert.That(account.Balance, Is.EqualTo(60m));
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionType.Withdrawal));
            Assert.That(transaction.Amount, Is.EqualTo(40m), "amounts are always positive; the type gives the direction");
            Assert.That(transaction.BalanceAfter, Is.EqualTo(60m));
        });
    }

    [Test]
    public void Withdraw_AllowsEmptyingTheAccount()
    {
        var account = TestData.Account(initialDeposit: 100m);
        account.Withdraw(100m, Now);
        Assert.That(account.Balance, Is.Zero);
    }

    [Test]
    public void Withdraw_RejectsAmountAboveBalance()
    {
        var account = TestData.Account(initialDeposit: 100m);

        var ex = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(100.01m, Now));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Balance, Is.EqualTo(100m));
            Assert.That(ex.Requested, Is.EqualTo(100.01m));
            Assert.That(account.Balance, Is.EqualTo(100m));
            Assert.That(account.Version, Is.EqualTo(1));
            Assert.That(account.Transactions, Has.Count.EqualTo(1));
        });
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Withdraw_RejectsNonPositiveAmount(decimal amount)
        => Assert.Throws<DomainException>(() => TestData.Account().Withdraw(amount, Now));

    [Test]
    public void ApplyInterest_CreditsTenPercentRoundedToCents()
    {
        var account = TestData.Account(initialDeposit: 100.25m);

        var transaction = account.ApplyInterest(Now);

        Assert.Multiple(() =>
        {
            Assert.That(transaction.TransactionType, Is.EqualTo(TransactionType.Interest));
            Assert.That(transaction.Amount, Is.EqualTo(10.02m), "10.025 rounds to even cents");
            Assert.That(account.Balance, Is.EqualTo(110.27m));
        });
    }

    [Test]
    public void ApplyInterest_RejectsWhenInterestRoundsToZero()
    {
        var account = BankAccount.Open("1234567890", 1, 10m, Now);
        account.Withdraw(9.96m, Now); // balance 0.04 -> interest 0.004 -> 0.00

        Assert.Throws<DomainException>(() => account.ApplyInterest(Now));
        Assert.That(account.Balance, Is.EqualTo(0.04m));
    }

    [Test]
    public void EveryOperation_BumpsVersionExactlyOnce()
    {
        var account = TestData.Account();
        account.Deposit(1m, Now);
        account.Withdraw(1m, Now);
        account.ApplyInterest(Now);

        Assert.That(account.Version, Is.EqualTo(4));
    }
}
