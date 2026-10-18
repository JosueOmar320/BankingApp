using System.ComponentModel.DataAnnotations;

namespace Banking.Application.Dtos;

/// <summary>Data required to open a bank account.</summary>
public sealed class CreateBankAccountDto
{
    /// <summary>Identifier of an existing customer.</summary>
    [Range(1, int.MaxValue)]
    public int CustomerId { get; init; }

    /// <summary>Opening deposit. Must be at least 10.00 and have at most two decimals.</summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal InitialDeposit { get; init; }
}
