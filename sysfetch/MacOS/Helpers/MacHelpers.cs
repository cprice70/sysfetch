using System.Text.Json;

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

    /// <summary>
    /// Parses `system_profiler -json SPDisplaysDataType` output into GPU names and
    /// display resolutions (physical pixels).
    /// </summary>
    public static (List<string> gpus, List<string> resolutions) ParseDisplays(string json)
    {
        var gpus = new List<string>();
        var resolutions = new List<string>();
        if (string.IsNullOrWhiteSpace(json)) return (gpus, resolutions);

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("SPDisplaysDataType", out var adapters)) return (gpus, resolutions);

            foreach (var adapter in adapters.EnumerateArray())
            {
                var model = GetString(adapter, "sppci_model") ?? GetString(adapter, "_name");
                if (!string.IsNullOrEmpty(model))
                {
                    var cores = GetString(adapter, "sppci_cores");
                    gpus.Add(string.IsNullOrEmpty(cores) ? model : $"{model} ({cores} cores)");
                }

                if (!adapter.TryGetProperty("spdisplays_ndrvs", out var displays)) continue;
                foreach (var display in displays.EnumerateArray())
                {
                    var pixels = GetString(display, "_spdisplays_pixels") ?? GetString(display, "_spdisplays_resolution");
                    if (!string.IsNullOrEmpty(pixels))
                        resolutions.Add(pixels.Replace(" x ", "x"));
                }
            }
        }
        catch (JsonException)
        {
            // Malformed output; return whatever was collected.
        }

        return (gpus, resolutions);
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
