using System.ComponentModel.DataAnnotations;
using Banking.Application.Resources;

namespace Banking.Application.Dtos;

/// <summary>Data required to open a bank account.</summary>
public sealed class CreateBankAccountDto
{
    /// <summary>Identifier of an existing customer.</summary>
    [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.CustomerIdRange))]
    public int CustomerId { get; init; }

    /// <summary>Opening deposit. Must be at least 10.00 and have at most two decimals.</summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.InitialDepositRange))]
    public decimal InitialDeposit { get; init; }
}
