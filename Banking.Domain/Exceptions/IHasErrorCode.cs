namespace Banking.Domain.Exceptions;

/// <summary>
/// An exception that identifies its cause with a stable code instead of relying on its message.
/// The outer layers use <see cref="Code"/> to pick a translated message and
/// <see cref="MessageArguments"/> to fill its placeholders, so the exception's own
/// <see cref="Exception.Message"/> (always English) is only used for logs and as a fallback.
/// </summary>
public interface IHasErrorCode
{
    /// <summary>Stable, language-neutral identifier of the error (e.g. <c>InsufficientFunds</c>).</summary>
    string Code { get; }

    /// <summary>Values to format into the translated message, in placeholder order.</summary>
    IReadOnlyList<object?> MessageArguments { get; }
}
