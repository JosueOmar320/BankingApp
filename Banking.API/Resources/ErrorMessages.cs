namespace Banking.API.Resources;

/// <summary>
/// Marker type for <c>IStringLocalizer&lt;ErrorMessages&gt;</c>. The texts live next to it in
/// <c>ErrorMessages.resx</c> (English, neutral) and <c>ErrorMessages.es.resx</c>; keys are the
/// problem title keys in <see cref="Localization.ProblemTitles"/> and the error codes of
/// <see cref="Banking.Domain.Exceptions.DomainErrorCodes"/>,
/// <see cref="Banking.Application.Exceptions.ApplicationErrorCodes"/> and
/// <see cref="Localization.ApiErrorCodes"/>.
/// </summary>
public sealed class ErrorMessages
{
}
