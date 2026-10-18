using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Options;

namespace Banking.API.Auth;

/// <summary>
/// Settings of the tokens the API issues and accepts, bound from the <c>Jwt</c> configuration section.
/// The signing key is a secret: <c>appsettings.Development.json</c> carries a development-only one,
/// and every other environment must supply its own (for example the <c>Jwt__SigningKey</c>
/// environment variable). The API refuses to start without a usable key.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinSigningKeyLength = 32;

    /// <summary>
    /// Text every development key contains. A key with this marker is rejected outside the
    /// Development environment, so the key committed to the repository can never protect real data.
    /// </summary>
    public const string DevelopmentKeyMarker = "DEVELOPMENT-ONLY";

    /// <summary>Value of the <c>iss</c> claim; tokens with another issuer are rejected.</summary>
    [Required]
    public string Issuer { get; init; } = "BankingApp";

    /// <summary>Value of the <c>aud</c> claim; tokens with another audience are rejected.</summary>
    [Required]
    public string Audience { get; init; } = "BankingApp.API";

    /// <summary>Secret used to sign (HS256) and validate tokens. At least 32 characters.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(MinSigningKeyLength)]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>How long an issued token stays valid.</summary>
    [Range(1, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    /// <summary>The lifetime as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenLifetimeMinutes);

    /// <summary>The signing key as the bytes the token handlers expect.</summary>
    public byte[] SigningKeyBytes => Encoding.UTF8.GetBytes(SigningKey);
}

/// <summary>Rejects the committed development key everywhere but in Development.</summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private readonly IHostEnvironment _environment;

    /// <summary>Creates the validator.</summary>
    public JwtOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!_environment.IsDevelopment()
            && options.SigningKey.Contains(JwtOptions.DevelopmentKeyMarker, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"Jwt:SigningKey is the development key, which is only accepted in the Development environment. "
                + $"Set a secret of at least {JwtOptions.MinSigningKeyLength} characters, for example through the Jwt__SigningKey environment variable.");
        }

        return ValidateOptionsResult.Success;
    }
}
