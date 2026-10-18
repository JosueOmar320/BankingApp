using Banking.Domain.Exceptions;

namespace Banking.Application.Exceptions;

/// <summary>
/// Raised when a requested or referenced resource does not exist.
/// The API maps it to a <c>404 Not Found</c> problem response whose text is chosen by <see cref="Code"/>.
/// </summary>
public sealed class NotFoundException : Exception, IHasErrorCode
{
    /// <summary>Generic code used when the exception is created without naming the resource.</summary>
    public const string DefaultCode = "NotFound";

    /// <summary>Creates the exception with a default message.</summary>
    public NotFoundException()
    {
    }

    /// <summary>Creates the exception with a custom message.</summary>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a custom message and inner exception.</summary>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates the exception for a resource looked up by key. The code is <c>{resource}NotFound</c>
    /// (see <see cref="ApplicationErrorCodes"/>), and the key is the message's only argument.
    /// </summary>
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
        Resource = resource;
        Key = key;
        Code = resource + DefaultCode;
        MessageArguments = [key];
    }

    /// <summary>Name of the missing resource (e.g. "Account").</summary>
    public string? Resource { get; }

    /// <summary>Key that was looked up.</summary>
    public object? Key { get; }

    /// <inheritdoc />
    public string Code { get; } = DefaultCode;

    /// <inheritdoc />
    public IReadOnlyList<object?> MessageArguments { get; } = [];
}
