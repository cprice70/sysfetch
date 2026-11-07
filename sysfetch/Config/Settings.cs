using System.Text;
using System.Text.Json;

namespace sysfetch.Config;

/// <summary>
/// Effective settings: defaults, overlaid by the config file, overlaid by CLI flags.
/// </summary>
public sealed class Settings
{
    public static readonly string[] DefaultModules =
        ["title", "underline", "os", "host", "kernel", "uptime", "resolution", "cpu", "gpu", "memory",
         "dotnet", "node", "python", "android", "git", "blank", "colors"];

    /// <summary>"auto", "none", or a logo name (apple, windows, linux, arch, ubuntu, debian, fedora).</summary>
    public string Logo { get; set; } = "auto";

    /// <summary>"auto", "always" or "never".</summary>
    public string Color { get; set; } = "auto";

    /// <summary>Key color name; null uses the logo's primary color.</summary>
    public string? KeyColor { get; set; }

    public string Separator { get; set; } = ": ";

    public List<string> Modules { get; set; } = [.. DefaultModules];

    public int TimeoutMs { get; set; } = 1000;

    public bool Json { get; set; }

    /// <summary>
    /// Default config location: $XDG_CONFIG_HOME/sysfetch/config.json, ~/.config/sysfetch/config.json,
    /// or %APPDATA%\sysfetch\config.json on Windows.
    /// </summary>
    public static string DefaultConfigPath(Func<string, string?> getEnv, bool isWindows)
    {
        if (isWindows)
        {
            var appData = getEnv("APPDATA") ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "sysfetch", "config.json");
        }

        var xdg = getEnv("XDG_CONFIG_HOME");
        var root = !string.IsNullOrEmpty(xdg)
            ? xdg
            : Path.Combine(getEnv("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(root, "sysfetch", "config.json");
    }

    /// <summary>
    /// Applies a JSON config (comments and trailing commas allowed) on top of these settings.
    /// Unknown keys are ignored; invalid values are reported in <paramref name="warnings"/>.
    /// </summary>
    public void ApplyJson(string json, List<string> warnings)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        }
        catch (JsonException ex)
        {
            warnings.Add($"config: invalid JSON ({ex.Message})");
            return;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("config: root must be an object");
                return;
            }

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "logo" when prop.Value.ValueKind == JsonValueKind.String:
                        Logo = prop.Value.GetString()!;
                        break;
                    case "color" when prop.Value.ValueKind == JsonValueKind.String:
                        SetColorMode(prop.Value.GetString()!, warnings);
                        break;
                    case "keyColor" when prop.Value.ValueKind == JsonValueKind.String:
                        var keyColor = prop.Value.GetString();
                        if (keyColor is "auto" or "") KeyColor = null;
                        else if (Output.Ansi.IsValidColor(keyColor)) KeyColor = keyColor;
                        else warnings.Add($"config: unknown keyColor '{keyColor}'");
                        break;
                    case "separator" when prop.Value.ValueKind == JsonValueKind.String:
                        Separator = prop.Value.GetString()!;
                        break;
                    case "timeoutMs" when prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var ms) && ms > 0:
                        TimeoutMs = ms;
                        break;
                    case "modules" when prop.Value.ValueKind == JsonValueKind.Array:
                        var modules = new List<string>();
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            var name = item.ValueKind == JsonValueKind.String ? item.GetString()! : string.Empty;
                            if (Output.TextRenderer.KnownModules.Contains(name, StringComparer.OrdinalIgnoreCase)) modules.Add(name);
                            else warnings.Add($"config: unknown module '{name}'");
                        }
                        Modules = modules;
                        break;
                    case "logo" or "color" or "keyColor" or "separator" or "timeoutMs" or "modules":
                        warnings.Add($"config: '{prop.Name}' has the wrong type");
                        break;
                }
            }
        }
    }

    public bool SetColorMode(string mode, List<string> warnings)
    {
        if (mode is "auto" or "always" or "never")
        {
            Color = mode;
            return true;
        }
        warnings.Add($"unknown color mode '{mode}' (use auto, always or never)");
        return false;
    }

    public static string DefaultConfigJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  // modules also accept: dotnet, node, python, android, git\n");
        sb.Append("  // auto, none, apple, windows, linux, arch, ubuntu, debian, fedora\n");
        sb.Append("  \"logo\": \"auto\",\n");
        sb.Append("  // auto, always, never (auto respects NO_COLOR and pipes)\n");
        sb.Append("  \"color\": \"auto\",\n");
        sb.Append("  // auto uses the logo color; or red, brightblue, 208, ...\n");
        sb.Append("  \"keyColor\": \"auto\",\n");
        sb.Append("  \"separator\": \": \",\n");
        sb.Append("  \"timeoutMs\": 1000,\n");
        sb.Append("  \"modules\": [");
        sb.Append(string.Join(", ", DefaultModules.Select(m => $"\"{m}\"")));
        sb.Append("]\n}\n");
        return sb.ToString();
    }
}
