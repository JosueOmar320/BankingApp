using Banking.Domain.Enums;
using Banking.Domain.Exceptions;
using Banking.Domain.Interfaces;

namespace Banking.Domain.Entities;

/// <summary>A bank customer. Create instances through <see cref="Create"/> so invariants hold.</summary>
public class Customer
{
    public const int MaxNameLength = 150;
    public const int MaxDocumentNumberLength = 50;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxPasswordHashLength = 256;

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

    /// <summary>
    /// One-way hash of the login password, produced by <see cref="IPasswordHasher"/>. The plain
    /// password is never stored. Empty for customers registered before passwords existed; they
    /// cannot log in until one is set.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Full display name.</summary>
    public string FullName => string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";

    /// <summary>Registers a new customer, validating every business invariant and hashing the password.</summary>
    /// <param name="password">Plain login password; only its hash is kept.</param>
    /// <param name="passwordHasher">Hashes the password.</param>
    /// <exception cref="DomainException">A value violates a business rule.</exception>
    public static Customer Create(
        string firstName,
        string? lastName,
        DateOnly birthDate,
        string documentNumber,
        Gender gender,
        decimal monthlyIncome,
        string password,
        IPasswordHasher passwordHasher,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        firstName = RequireText(firstName, "First name", MaxNameLength, DomainErrorCodes.FirstNameRequired, DomainErrorCodes.FirstNameTooLong);
        lastName = string.IsNullOrWhiteSpace(lastName) ? null : LimitLength(lastName, "Last name", MaxNameLength, DomainErrorCodes.LastNameTooLong);
        documentNumber = RequireText(documentNumber, "Document number", MaxDocumentNumberLength, DomainErrorCodes.DocumentNumberRequired, DomainErrorCodes.DocumentNumberTooLong);
        EnsureValidPassword(password);

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
            PasswordHash = passwordHasher.Hash(password),
            CreatedAt = createdAtUtc,
        };
    }

    /// <summary>Checks a login attempt. Always false for a customer without a password.</summary>
    public bool VerifyPassword(string password, IPasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(passwordHasher);

        return !string.IsNullOrEmpty(PasswordHash)
            && !string.IsNullOrEmpty(password)
            && passwordHasher.Verify(PasswordHash, password);
    }

    /// <summary>
    /// The password policy: a plain length window. Passwords are never trimmed, so a leading or
    /// trailing space is part of the secret.
    /// </summary>
    private static void EnsureValidPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainException(DomainErrorCodes.PasswordRequired, "Password is required.");
        }

        if (password.Length < MinPasswordLength)
        {
            throw new DomainException(DomainErrorCodes.PasswordTooShort, $"Password must be at least {MinPasswordLength} characters long.", MinPasswordLength);
        }

        if (password.Length > MaxPasswordLength)
        {
            throw new DomainException(DomainErrorCodes.PasswordTooLong, $"Password cannot be longer than {MaxPasswordLength} characters.", MaxPasswordLength);
        }
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
