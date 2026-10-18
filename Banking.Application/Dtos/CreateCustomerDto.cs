using System.ComponentModel.DataAnnotations;
using Banking.Application.Resources;
using Banking.Domain.Entities;
using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

/// <summary>Data required to register a customer.</summary>
public sealed class CreateCustomerDto
{
    /// <summary>Given name.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.FirstNameRequired))]
    [StringLength(Customer.MaxNameLength, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.FirstNameLength))]
    public required string FirstName { get; init; }

    /// <summary>Family name (optional).</summary>
    [StringLength(Customer.MaxNameLength, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.LastNameLength))]
    public string? LastName { get; init; }

    /// <summary>Date of birth (yyyy-MM-dd). Cannot be in the future.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.BirthDateRequired))]
    public DateOnly BirthDate { get; init; }

    /// <summary>Identification document number. Must be unique.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.DocumentNumberRequired))]
    [StringLength(Customer.MaxDocumentNumberLength, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.DocumentNumberLength))]
    public required string DocumentNumber { get; init; }

    /// <summary>Gender: <c>M</c>, <c>F</c> or <c>O</c>.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.GenderRequired))]
    [EnumDataType(typeof(Gender), ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.GenderInvalid))]
    public Gender Gender { get; init; }

    /// <summary>Declared monthly income. Cannot be negative.</summary>
    [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.MonthlyIncomeRange))]
    public decimal MonthlyIncome { get; init; }

    /// <summary>Login password, between 8 and 128 characters. Stored only as a hash.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordRequired))]
    [StringLength(Customer.MaxPasswordLength, MinimumLength = Customer.MinPasswordLength, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordLength))]
    public required string Password { get; init; }
}
