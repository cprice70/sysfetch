namespace sysfetch.MacOS.Helpers;

public class MacHelpers
{
    public static string GetDistroInfo(string osxVersion, string osxBuild, string distroShorthand)
    {
        
        var distro = distroShorthand switch
        {
            "off" => $"macOS {osxVersion} {osxBuild}",
            "on" or "tiny" => $"macOS {osxVersion}",
            _ => $"macOS {osxVersion} {osxBuild}"
        };

        return distro;
    }
}