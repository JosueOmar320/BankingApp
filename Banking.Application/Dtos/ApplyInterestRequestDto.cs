using System.ComponentModel.DataAnnotations;
using Banking.Application.Resources;

namespace Banking.Application.Dtos;

/// <summary>Request to credit interest on an account.</summary>
public sealed class ApplyInterestRequestDto
{
    /// <summary>Account number to operate on.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountNumberRequired))]
    [StringLength(32, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountNumberLength))]
    public required string AccountNumber { get; init; }
}
