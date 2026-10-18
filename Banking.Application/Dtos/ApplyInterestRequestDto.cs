using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

/// <summary>Request to credit interest on an account.</summary>
public sealed class ApplyInterestRequestDto
{
    /// <summary>Account number to operate on.</summary>
    [Required]
    [StringLength(32, MinimumLength = 1)]
    public required string AccountNumber { get; init; }
}
