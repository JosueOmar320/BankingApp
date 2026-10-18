using Banking.API.Auth;
using Microsoft.Extensions.Options;

namespace Banking.Tests.API;

/// <summary>The API refuses to start without a real JWT signing key, so a misconfigured deployment cannot issue tokens.</summary>
public class StartupConfigurationTests
{
    [Test]
    public void Host_FailsToStart_WhenTheSigningKeyIsMissing()
    {
        using var factory = new BankingApiFactory(signingKey: null);

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.That(ex!.Message, Does.Contain(nameof(JwtOptions.SigningKey)));
    }

    [Test]
    public void Host_FailsToStart_WhenTheSigningKeyIsTooShort()
    {
        using var factory = new BankingApiFactory(signingKey: "too-short");

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Test]
    public void Host_FailsToStart_WithTheDevelopmentKey_OutsideDevelopment()
    {
        using var factory = new BankingApiFactory(environment: "Production", signingKey: $"{JwtOptions.DevelopmentKeyMarker}-0123456789-0123456789-0123456789");

        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.That(ex!.Message, Does.Contain("Development"));
    }

    [Test]
    public void Host_Starts_WithTheDevelopmentKey_InDevelopment()
    {
        using var factory = new BankingApiFactory(environment: "Development", signingKey: $"{JwtOptions.DevelopmentKeyMarker}-0123456789-0123456789-0123456789");

        Assert.DoesNotThrow(() => factory.CreateClient().Dispose());
    }
}
