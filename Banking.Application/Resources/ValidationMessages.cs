using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Banking.Application.Resources;

/// <summary>
/// Strongly-typed access to <c>ValidationMessages.resx</c> (English, neutral) and
/// <c>ValidationMessages.es.resx</c>. DataAnnotations attributes on the request DTOs reference
/// these properties through <c>ErrorMessageResourceType</c>/<c>ErrorMessageResourceName</c>, so the
/// validation text follows <see cref="CultureInfo.CurrentUICulture"/> without the application layer
/// depending on ASP.NET Core. Each property name is its resource key.
/// </summary>
internal static class ValidationMessages
{
    private static readonly ResourceManager Resources = new(
        "Banking.Application.Resources.ValidationMessages",
        typeof(ValidationMessages).Assembly);

    public static string AccountNumberRequired => Get();

    public static string AccountNumberLength => Get();

    public static string AmountRange => Get();

    public static string CustomerIdRange => Get();

    public static string InitialDepositRange => Get();

    public static string FirstNameRequired => Get();

    public static string FirstNameLength => Get();

    public static string LastNameLength => Get();

    public static string BirthDateRequired => Get();

    public static string DocumentNumberRequired => Get();

    public static string DocumentNumberLength => Get();

    public static string GenderRequired => Get();

    public static string GenderInvalid => Get();

    public static string MonthlyIncomeRange => Get();

    public static string PasswordRequired => Get();

    public static string PasswordLength => Get();

    public static string PageRange => Get();

    public static string PageSizeRange => Get();

    /// <summary>Reads the resource for the given culture, or the invariant text when the culture has none.</summary>
    public static string? GetString(string key, CultureInfo culture) => Resources.GetString(key, culture);

    private static string Get([CallerMemberName] string key = "")
        => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
