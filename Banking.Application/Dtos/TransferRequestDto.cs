using System.ComponentModel.DataAnnotations;
using Banking.Application.Resources;

namespace Banking.Application.Dtos;

/// <summary>A transfer from one of the caller's accounts to any other account.</summary>
public sealed class TransferRequestDto
{
    /// <summary>Account the money leaves; it must belong to the authenticated customer.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountNumberRequired))]
    [StringLength(32, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountNumberLength))]
    public required string SourceAccountNumber { get; init; }

    /// <summary>Account that receives the money; it may belong to another customer.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountNumberRequired))]
    [StringLength(32, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AccountNumberLength))]
    public required string DestinationAccountNumber { get; init; }

    /// <summary>Amount to move. Must be positive and have at most two decimals.</summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.AmountRange))]
    public decimal Amount { get; init; }
}
