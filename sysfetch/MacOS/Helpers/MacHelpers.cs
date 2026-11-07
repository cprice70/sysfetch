namespace sysfetch.MacOS.Helpers;

public static class MacHelpers
{
    public static string GetDistroInfo(string osxVersion, string osxBuild, string distroShorthand)
    {
        var distro = distroShorthand switch
        {
            "on" or "tiny" => $"macOS {osxVersion}",
            _ => $"macOS {osxVersion} {osxBuild}"
        };

        return distro;
    }
}
