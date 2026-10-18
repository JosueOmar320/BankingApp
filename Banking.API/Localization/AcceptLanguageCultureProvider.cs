using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Net.Http.Headers;

namespace Banking.API.Localization;

/// <summary>
/// Picks the response culture from the <c>Accept-Language</c> header. Unlike the built-in provider,
/// a language without a region (<c>es</c>, <c>en</c>) or with a region the API does not have texts
/// for (<c>es-MX</c>, <c>en-GB</c>) maps to the supported culture of the same language
/// (<c>es-NI</c>, <c>en-US</c>), so any Spanish-speaking client gets Spanish. Languages are tried in
/// the header's quality order; when none matches, the default culture applies.
/// </summary>
public sealed class AcceptLanguageCultureProvider : RequestCultureProvider
{
    private const int MaxLanguagesToTry = 5;

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var languages = httpContext.Request.GetTypedHeaders().AcceptLanguage;
        if (languages is null || languages.Count == 0)
        {
            return NullProviderCultureResult;
        }

        IReadOnlyList<CultureInfo> supported = Options?.SupportedUICultures is { } configured ? configured.ToList() : SupportedCultures.All;
        var candidates = languages
            .OrderByDescending(language => language.Quality ?? 1.0)
            .Take(MaxLanguagesToTry)
            .Select(language => language.Value.ToString());

        foreach (var candidate in candidates)
        {
            var match = Match(candidate, supported);
            if (match is not null)
            {
                return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(match.Name));
            }
        }

        return NullProviderCultureResult;
    }

    private static CultureInfo? Match(string requested, IReadOnlyList<CultureInfo> supported)
    {
        if (string.IsNullOrWhiteSpace(requested) || requested == "*")
        {
            return null;
        }

        var exact = supported.FirstOrDefault(culture => string.Equals(culture.Name, requested, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        // "es", "es-MX" → the supported culture whose language is "es".
        var language = requested.Split('-', 2)[0];
        return supported.FirstOrDefault(culture => string.Equals(culture.TwoLetterISOLanguageName, language, StringComparison.OrdinalIgnoreCase));
    }
}
