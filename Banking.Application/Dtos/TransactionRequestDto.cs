using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

/// <summary>A deposit or withdrawal request.</summary>
public sealed class TransactionRequestDto
{
    /// <summary>Account number to operate on.</summary>
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public required string AccountNumber { get; init; }

    /// <summary>Amount to move. Must be positive and have at most two decimals.</summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; init; }
}
