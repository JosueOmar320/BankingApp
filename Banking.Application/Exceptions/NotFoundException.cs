namespace Banking.Application.Exceptions;

/// <summary>
/// Raised when a requested or referenced resource does not exist.
/// The API maps it to a <c>404 Not Found</c> problem response.
/// </summary>
public sealed class NotFoundException : Exception
{
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

    /// <summary>Creates the exception for a resource looked up by key.</summary>
    public NotFoundException(string resource, object key)
        : base($"{resource} '{key}' was not found.")
    {
        Resource = resource;
        Key = key;
    }

    /// <summary>Name of the missing resource (e.g. "Account").</summary>
    public string? Resource { get; }

    /// <summary>Key that was looked up.</summary>
    public object? Key { get; }
}
