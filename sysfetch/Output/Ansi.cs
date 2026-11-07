namespace sysfetch.Output;

public static class Ansi
{
    public const string Reset = "\u001b[0m";
    public const string Bold = "\u001b[1m";

    private static readonly string[] Names =
        ["black", "red", "green", "yellow", "blue", "magenta", "cyan", "white"];

    /// <summary>
    /// Foreground escape for a color name: "red", "brightblue", "default", or a
    /// 256-color index such as "208". Returns null for unknown names.
    /// </summary>
    public static string? Foreground(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        name = name.Trim().ToLowerInvariant();

        if (name == "default") return "\u001b[39m";
        if (int.TryParse(name, out var index) && index is >= 0 and <= 255) return $"\u001b[38;5;{index}m";

        var bright = name.StartsWith("bright", StringComparison.Ordinal);
        var baseName = bright ? name["bright".Length..] : name;
        var code = Array.IndexOf(Names, baseName);
        if (code < 0) return null;

        return $"\u001b[{(bright ? 90 : 30) + code}m";
    }

    public static bool IsValidColor(string? name) => Foreground(name) is not null;

    /// <summary>Length of a string as displayed, ignoring ANSI escape sequences.</summary>
    public static int VisibleLength(string text)
    {
        var length = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\u001b')
            {
                // Skip CSI sequence: ESC [ ... final byte (0x40-0x7E)
                i++;
                if (i < text.Length && text[i] == '[')
                {
                    while (++i < text.Length && text[i] is < '@' or > '~') { }
                }
                continue;
            }
            length++;
        }
        return length;
    }

    /// <summary>
    /// Decides whether to emit color. "always"/"never" win; "auto" honours NO_COLOR,
    /// TERM=dumb and output redirection.
    /// </summary>
    public static bool ShouldUseColor(string mode, bool outputRedirected, Func<string, string?> getEnv)
    {
        switch (mode.ToLowerInvariant())
        {
            case "always": return true;
            case "never": return false;
        }

        if (!string.IsNullOrEmpty(getEnv("NO_COLOR"))) return false;
        if (string.Equals(getEnv("TERM"), "dumb", StringComparison.Ordinal)) return false;
        return !outputRedirected;
    }
}
