using System.Globalization;

namespace Banking.API.Localization;

/// <summary>The cultures the API can answer in. English is the default.</summary>
public static class SupportedCultures
{
    /// <summary>Culture used when the request carries no usable <c>Accept-Language</c>.</summary>
    public const string Default = "en-US";

    /// <summary>Spanish as spoken in Nicaragua, where the project is maintained.</summary>
    public const string Spanish = "es-NI";

    /// <summary>Every culture with its own texts, number and date formats.</summary>
    public static IReadOnlyList<CultureInfo> All { get; } =
    [
        new CultureInfo(Default),
        new CultureInfo(Spanish),
    ];

    /// <summary>Values documented in Swagger for the <c>Accept-Language</c> header.</summary>
    public static IReadOnlyList<string> AcceptLanguageExamples { get; } = [Default, "en", Spanish, "es"];
}
