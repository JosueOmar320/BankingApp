using System.ComponentModel.DataAnnotations;
using Banking.Application.Resources;
using Banking.Domain.Entities;

namespace Banking.Application.Dtos;

/// <summary>Credentials of a registered customer.</summary>
public sealed class LoginRequestDto
{
    /// <summary>Identification document number used at registration.</summary>
    [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.DocumentNumberRequired))]
    [StringLength(Customer.MaxDocumentNumberLength, MinimumLength = 1, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.DocumentNumberLength))]
    public required string DocumentNumber { get; init; }

    /// <summary>Login password.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PasswordRequired))]
    public required string Password { get; init; }
}
