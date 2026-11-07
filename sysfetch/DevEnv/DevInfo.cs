namespace sysfetch.DevEnv;

/// <summary>Developer toolchain details. Null members mean "not found".</summary>
public sealed class DevInfo
{
    public string? DotnetSdk { get; set; }
    public int DotnetSdkCount { get; set; }

    public string? Node { get; set; }

    public string? Python { get; set; }
    /// <summary>Where the Python version came from: "venv", "pyenv" or null for PATH.</summary>
    public string? PythonSource { get; set; }

    public string? AndroidApi { get; set; }
    public string? AndroidBuildTools { get; set; }

    public string? GitRepo { get; set; }
    public string? GitBranch { get; set; }

    public string? FormatDotnet() =>
        DotnetSdk is null ? null
        : DotnetSdkCount > 1 ? $"SDK {DotnetSdk} ({DotnetSdkCount} installed)"
        : $"SDK {DotnetSdk}";

    public string? FormatPython() =>
        Python is null ? null : PythonSource is null ? Python : $"{Python} ({PythonSource})";

    public string? FormatAndroid()
    {
        if (AndroidApi is null && AndroidBuildTools is null) return null;
        if (AndroidBuildTools is null) return $"API {AndroidApi}";
        if (AndroidApi is null) return $"build-tools {AndroidBuildTools}";
        return $"API {AndroidApi}, build-tools {AndroidBuildTools}";
    }

    public string? FormatGit() =>
        GitRepo is null ? null : GitBranch is null ? GitRepo : $"{GitRepo} @ {GitBranch}";
}
