using Banking.Domain.Enums;

namespace Banking.Application.Dtos;

/// <summary>A registered customer.</summary>
/// <param name="CustomerId">Identifier assigned by the system.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name, when provided.</param>
/// <param name="BirthDate">Date of birth.</param>
/// <param name="DocumentNumber">Identification document number.</param>
/// <param name="Gender">Gender: <c>M</c>, <c>F</c> or <c>O</c>.</param>
/// <param name="MonthlyIncome">Declared monthly income.</param>
/// <param name="CreatedAt">UTC registration timestamp.</param>
public sealed record CustomerResponseDto(
    int CustomerId,
    string FirstName,
    string? LastName,
    DateOnly BirthDate,
    string DocumentNumber,
    Gender Gender,
    decimal MonthlyIncome,
    DateTime CreatedAt);
