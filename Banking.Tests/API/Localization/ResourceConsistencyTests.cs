using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using Banking.API.Localization;
using Banking.API.Resources;
using Banking.Application.Exceptions;
using Banking.Application.Resources;
using Banking.Domain.Exceptions;

namespace Banking.Tests.API.Localization;

/// <summary>
/// Guards the contract between the code and the <c>.resx</c> files: every key the code can ask for
/// exists in the English (neutral) resources and in the Spanish ones, both files declare the same
/// keys, and each translation uses the same placeholders as its English counterpart.
/// </summary>
public class ResourceConsistencyTests
{
    private static readonly CultureInfo Spanish = new("es");

    private static readonly ResourceManager ErrorResources = new(
        "Banking.API.Resources.ErrorMessages",
        typeof(ErrorMessages).Assembly);

    private static readonly ResourceManager ValidationResources = new(
        "Banking.Application.Resources.ValidationMessages",
        typeof(ValidationMessages).Assembly);

    /// <summary>Every key <c>GlobalExceptionHandler</c> and <c>ValidationProblemResponse</c> can look up.</summary>
    private static IEnumerable<string> ErrorMessageKeysUsedByCode()
    {
        foreach (var key in Constants(typeof(ProblemTitles)))
        {
            yield return key;
        }

        foreach (var key in Constants(typeof(DomainErrorCodes)))
        {
            yield return key;
        }

        foreach (var key in Constants(typeof(ApplicationErrorCodes)))
        {
            yield return key;
        }

        yield return NotFoundException.DefaultCode;

        // Of the API codes, only the cancelled request carries a translated detail: validation
        // failures explain themselves in the errors map and unexpected errors return no detail.
        yield return ApiErrorCodes.RequestCancelled;
    }

    private static IEnumerable<string> ValidationMessageKeysUsedByCode()
        => typeof(ValidationMessages)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(string))
            .Select(property => property.Name);

    [TestCaseSource(nameof(ErrorMessageKeysUsedByCode))]
    public void ErrorMessageKey_ExistsInEnglishAndSpanish(string key)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Keys(ErrorResources, CultureInfo.InvariantCulture), Does.Contain(key), "missing in ErrorMessages.resx");
            Assert.That(Keys(ErrorResources, Spanish), Does.Contain(key), "missing in ErrorMessages.es.resx");
        });
    }

    [TestCaseSource(nameof(ValidationMessageKeysUsedByCode))]
    public void ValidationMessageKey_ExistsInEnglishAndSpanish(string key)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Keys(ValidationResources, CultureInfo.InvariantCulture), Does.Contain(key), "missing in ValidationMessages.resx");
            Assert.That(Keys(ValidationResources, Spanish), Does.Contain(key), "missing in ValidationMessages.es.resx");
        });
    }

    [Test]
    public void ErrorMessages_EnglishAndSpanish_DeclareTheSameKeys()
        => Assert.That(Keys(ErrorResources, Spanish), Is.EquivalentTo(Keys(ErrorResources, CultureInfo.InvariantCulture)));

    [Test]
    public void ValidationMessages_EnglishAndSpanish_DeclareTheSameKeys()
        => Assert.That(Keys(ValidationResources, Spanish), Is.EquivalentTo(Keys(ValidationResources, CultureInfo.InvariantCulture)));

    [Test]
    public void ErrorMessages_Translations_UseTheSamePlaceholdersAsEnglish()
        => AssertSamePlaceholders(ErrorResources);

    [Test]
    public void ValidationMessages_Translations_UseTheSamePlaceholdersAsEnglish()
        => AssertSamePlaceholders(ValidationResources);

    [Test]
    public void ValidationMessages_FollowTheCurrentUICulture()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("es-NI");
            Assert.That(ValidationMessages.AmountRange, Is.EqualTo("El monto debe ser mayor que cero."));

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.That(ValidationMessages.AmountRange, Is.EqualTo("The amount must be greater than zero."));
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    private static void AssertSamePlaceholders(ResourceManager resources)
    {
        var english = Entries(resources, CultureInfo.InvariantCulture);
        var spanish = Entries(resources, Spanish);

        Assert.Multiple(() =>
        {
            foreach (var (key, text) in english)
            {
                Assert.That(Placeholders(spanish[key]), Is.EquivalentTo(Placeholders(text)), $"placeholders differ for '{key}'");
            }
        });
    }

    private static IEnumerable<string> Placeholders(string text)
        => Regex.Matches(text, @"\{(\d+)[^}]*\}").Select(match => match.Groups[1].Value);

    private static IEnumerable<string> Constants(Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);

    private static List<string> Keys(ResourceManager resources, CultureInfo culture)
        => Entries(resources, culture).Keys.ToList();

    private static Dictionary<string, string> Entries(ResourceManager resources, CultureInfo culture)
    {
        // tryParents: false, so the Spanish set contains only what ErrorMessages.es.resx declares.
        var set = resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        Assert.That(set, Is.Not.Null, $"no resources for culture '{culture.Name}'");

        return set!.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }
}
