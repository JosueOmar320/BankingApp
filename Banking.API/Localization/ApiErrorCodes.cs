namespace Banking.API.Localization;

/// <summary>
/// Error codes produced by the API layer itself (the domain and application layers have their own).
/// They are returned in the <c>code</c> extension of every problem response.
/// </summary>
public static class ApiErrorCodes
{
    /// <summary>The request payload failed DataAnnotations validation; see the <c>errors</c> map.</summary>
    public const string ValidationFailed = nameof(ValidationFailed);

    /// <summary>The request has no bearer token, or the token is expired or tampered with.</summary>
    public const string AuthenticationRequired = nameof(AuthenticationRequired);

    /// <summary>The client disconnected before the request completed.</summary>
    public const string RequestCancelled = nameof(RequestCancelled);

    /// <summary>An unhandled exception; details are logged, never returned.</summary>
    public const string UnexpectedError = nameof(UnexpectedError);
}
