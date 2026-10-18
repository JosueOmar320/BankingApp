using System.ComponentModel.DataAnnotations;
using Banking.Domain.Entities;
using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

/// <summary>Data required to register a customer.</summary>
public sealed class CreateCustomerDto
{
    /// <summary>Given name.</summary>
    [Required]
    [StringLength(Customer.MaxNameLength, MinimumLength = 1)]
    public required string FirstName { get; init; }

    /// <summary>Family name (optional).</summary>
    [StringLength(Customer.MaxNameLength)]
    public string? LastName { get; init; }

    /// <summary>Date of birth (yyyy-MM-dd). Cannot be in the future.</summary>
    [Required]
    public DateOnly BirthDate { get; init; }

    /// <summary>Identification document number. Must be unique.</summary>
    [Required]
    [StringLength(Customer.MaxDocumentNumberLength, MinimumLength = 1)]
    public required string DocumentNumber { get; init; }

    /// <summary>Gender: <c>M</c>, <c>F</c> or <c>O</c>.</summary>
    [Required]
    [EnumDataType(typeof(Gender))]
    public Gender Gender { get; init; }

    /// <summary>Declared monthly income. Cannot be negative.</summary>
    [Range(0, double.MaxValue)]
    public decimal MonthlyIncome { get; init; }
}
