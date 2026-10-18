using Banking.Domain.Exceptions;

namespace Banking.Application.Exceptions;

/// <summary>
/// Raised when the authenticated customer asks for data that belongs to another customer.
/// The API maps it to a <c>403 Forbidden</c> problem response whose text is chosen by <see cref="Code"/>.
/// </summary>
public sealed class ForbiddenException : Exception, IHasErrorCode
{
    /// <summary>Generic code used when the exception is created without naming the resource.</summary>
    public const string DefaultCode = "AccessDenied";

    /// <summary>Creates the exception with a default message.</summary>
    public ForbiddenException()
        : base("Access to the requested resource is denied.")
    {
    }

    /// <summary>Creates the exception with a custom message.</summary>
    public ForbiddenException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a custom message and inner exception.</summary>
    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates the exception for a resource identified by key. The code is <c>{resource}AccessDenied</c>
    /// (see <see cref="ApplicationErrorCodes"/>), and the key is the message's only argument.
    /// </summary>
    public ForbiddenException(string resource, object key)
        : base($"{resource} '{key}' belongs to another customer.")
    {
        Resource = resource;
        Key = key;
        Code = resource + DefaultCode;
        MessageArguments = [key];
    }

    /// <summary>Name of the protected resource (e.g. "Account").</summary>
    public string? Resource { get; }

    /// <summary>Key of the protected resource.</summary>
    public object? Key { get; }

    /// <inheritdoc />
    public string Code { get; } = DefaultCode;

    /// <inheritdoc />
    public IReadOnlyList<object?> MessageArguments { get; } = [];
}
