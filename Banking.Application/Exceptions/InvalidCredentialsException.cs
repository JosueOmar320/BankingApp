using Banking.Domain.Exceptions;

namespace Banking.Application.Exceptions;

/// <summary>
/// Raised when a login attempt fails. The message never says whether the document number or the
/// password was wrong. The API maps it to a <c>401 Unauthorized</c> problem response.
/// </summary>
public sealed class InvalidCredentialsException : Exception, IHasErrorCode
{
    /// <summary>Creates the exception with the default, non-revealing message.</summary>
    public InvalidCredentialsException()
        : base("The document number or the password is incorrect.")
    {
    }

    /// <summary>Creates the exception with a custom message.</summary>
    public InvalidCredentialsException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a custom message and inner exception.</summary>
    public InvalidCredentialsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <inheritdoc />
    public string Code => ApplicationErrorCodes.InvalidCredentials;

    /// <inheritdoc />
    public IReadOnlyList<object?> MessageArguments => [];
}
