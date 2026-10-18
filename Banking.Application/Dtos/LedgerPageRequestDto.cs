using System.ComponentModel.DataAnnotations;
using Banking.Application.Resources;

namespace Banking.Application.Dtos;

/// <summary>Which page of an account's ledger to return.</summary>
public sealed class LedgerPageRequestDto
{
    /// <summary>Page size used when the request does not name one.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>Largest page a client may ask for.</summary>
    public const int MaxPageSize = 200;

    /// <summary>1-based page number. Defaults to the first page.</summary>
    [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PageRange))]
    public int Page { get; init; } = 1;

    /// <summary>Transactions per page, between 1 and 200. Defaults to 50.</summary>
    [Range(1, MaxPageSize, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = nameof(ValidationMessages.PageSizeRange))]
    public int PageSize { get; init; } = DefaultPageSize;
}
