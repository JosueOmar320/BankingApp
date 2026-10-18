using Banking.Domain.Enums;
using Banking.Domain.Exceptions;

namespace Banking.Domain.Entities;

/// <summary>A bank customer. Create instances through <see cref="Create"/> so invariants hold.</summary>
public class Customer
{
    public const int MaxNameLength = 150;
    public const int MaxDocumentNumberLength = 50;

    // EF Core materialisation.
    private Customer()
    {
    }

    public int CustomerId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string? LastName { get; private set; }

    public DateOnly BirthDate { get; private set; }

    /// <summary>Identification document number. Unique across customers.</summary>
    public string DocumentNumber { get; private set; } = null!;

    public Gender Gender { get; private set; }

    public decimal MonthlyIncome { get; private set; }

    /// <summary>UTC timestamp of registration.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Full display name.</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";

    /// <summary>Registers a new customer, validating every business invariant.</summary>
    /// <exception cref="DomainException">A value violates a business rule.</exception>
    public static Customer Create(
        string firstName,
        string? lastName,
        DateOnly birthDate,
        string documentNumber,
        Gender gender,
        decimal monthlyIncome,
        DateTime createdAtUtc)
    {
        firstName = RequireText(firstName, "First name", MaxNameLength, DomainErrorCodes.FirstNameRequired, DomainErrorCodes.FirstNameTooLong);
        lastName = string.IsNullOrWhiteSpace(lastName) ? null : LimitLength(lastName, "Last name", MaxNameLength, DomainErrorCodes.LastNameTooLong);
        documentNumber = RequireText(documentNumber, "Document number", MaxDocumentNumberLength, DomainErrorCodes.DocumentNumberRequired, DomainErrorCodes.DocumentNumberTooLong);

        if (!Enum.IsDefined(gender))
        {
            throw new DomainException(DomainErrorCodes.InvalidGender, "Gender must be M, F or O.");
        }

        if (birthDate > DateOnly.FromDateTime(createdAtUtc))
        {
            throw new DomainException(DomainErrorCodes.BirthDateInFuture, "Birth date cannot be in the future.");
        }

        if (monthlyIncome < 0)
        {
            throw new DomainException(DomainErrorCodes.NegativeMonthlyIncome, "Monthly income cannot be negative.");
        }

        return new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            BirthDate = birthDate,
            DocumentNumber = documentNumber,
            Gender = gender,
            MonthlyIncome = monthlyIncome,
            CreatedAt = createdAtUtc,
        };
    }

    private static string RequireText(string? value, string field, int maxLength, string requiredCode, string tooLongCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(requiredCode, $"{field} is required.");
        }

        return LimitLength(value, field, maxLength, tooLongCode);
    }

    private static string LimitLength(string value, string field, int maxLength, string tooLongCode)
    {
        value = value.Trim();
        if (value.Length > maxLength)
        {
            throw new DomainException(tooLongCode, $"{field} cannot be longer than {maxLength} characters.", maxLength);
        }

        return value;
    }
}
