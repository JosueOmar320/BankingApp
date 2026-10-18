using System.Reflection;

namespace Banking.API;

/// <summary>
/// The version of the running API, read from the assembly so Swagger, the binaries and the release tag always agree.
/// </summary>
public static class ApiVersion
{
    /// <summary>
    /// The semantic version stamped by <c>&lt;Version&gt;</c> in <c>Directory.Build.props</c> (for example
    /// <c>1.0.0</c>), without the <c>+commit</c> build metadata that Source Link appends to CI builds.
    /// </summary>
    public static string Current { get; } = Read();

    private static string Read()
    {
        var assembly = typeof(ApiVersion).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var metadata = informational.IndexOf('+', StringComparison.Ordinal);
            return metadata < 0 ? informational : informational[..metadata];
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
