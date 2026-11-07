using System.Text;
using sysfetch.Config;

namespace sysfetch.Output;

public static class TextRenderer
{
    public static readonly string[] KnownModules =
        ["title", "underline", "os", "host", "kernel", "uptime", "resolution", "cpu", "gpu", "memory", "blank", "colors"];

    public static string Render(SysInfo info, Settings settings, bool useColor, string user, string hostName)
    {
        var logo = ResolveLogo(info, settings.Logo);
        var accent = Ansi.Foreground(settings.KeyColor)
                     ?? (logo is not null ? Ansi.Foreground(logo.Colors[0]) : null)
                     ?? Ansi.Foreground("blue");

        var lines = BuildInfoLines(info, settings, useColor, accent!, user, hostName);
        return Compose(logo, lines, useColor);
    }

    public static Logo? ResolveLogo(SysInfo info, string logoSetting) =>
        logoSetting.ToLowerInvariant() switch
        {
            "none" or "off" => null,
            "auto" or "" => Logos.ForOs(info.OSId, info.OSIdLike),
            var name => Logos.ByName(name) ?? Logos.ForOs(info.OSId, info.OSIdLike)
        };

    public static List<string> BuildInfoLines(SysInfo info, Settings settings, bool useColor, string accent, string user, string hostName)
    {
        var lines = new List<string>();
        string Key(string key) => useColor ? $"{Ansi.Bold}{accent}{key}{Ansi.Reset}" : key;
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add($"{Key(key)}{settings.Separator}{value}");
        }
        void AddAll(string key, IEnumerable<string?>? values)
        {
            if (values is null) return;
            foreach (var value in values) Add(key, value);
        }

        var title = $"{user}@{hostName}";
        foreach (var module in settings.Modules)
        {
            switch (module.ToLowerInvariant())
            {
                case "title":
                    lines.Add(useColor
                        ? $"{Ansi.Bold}{accent}{user}{Ansi.Reset}@{Ansi.Bold}{accent}{hostName}{Ansi.Reset}"
                        : title);
                    break;
                case "underline":
                    lines.Add(new string('-', title.Length));
                    break;
                case "os": Add("OS", info.OSName); break;
                case "host": Add("Host", info.Host); break;
                case "kernel": Add("Kernel", info.Kernel); break;
                case "uptime": Add("Uptime", info.Uptime); break;
                case "resolution": AddAll("Resolution", info.Resolution); break;
                case "cpu": AddAll("CPU", info.CPU); break;
                case "gpu": AddAll("GPU", info.GPUs); break;
                case "memory": Add("Memory", info.Memory); break;
                case "blank": lines.Add(string.Empty); break;
                case "colors":
                    if (useColor) lines.AddRange(ColorBlocks());
                    break;
            }
        }

        while (lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    private static IEnumerable<string> ColorBlocks()
    {
        var normal = new StringBuilder();
        var bright = new StringBuilder();
        for (var i = 0; i < 8; i++)
        {
            normal.Append($"\u001b[4{i}m   ");
            bright.Append($"\u001b[10{i}m   ");
        }
        yield return normal.Append(Ansi.Reset).ToString();
        yield return bright.Append(Ansi.Reset).ToString();
    }

    public static string Compose(Logo? logo, IReadOnlyList<string> infoLines, bool useColor)
    {
        var sb = new StringBuilder();
        if (logo is null)
        {
            foreach (var line in infoLines) sb.Append(line).Append('\n');
            return sb.ToString();
        }

        const int gap = 3;
        var width = logo.Width + gap;
        var rows = Math.Max(logo.Lines.Length, infoLines.Count);

        for (var i = 0; i < rows; i++)
        {
            var logoLine = i < logo.Lines.Length ? logo.Lines[i] : string.Empty;
            var visible = Logos.StripMarkers(logoLine).Length;
            var rendered = Logos.Colorize(logoLine, logo.Colors, useColor);
            if (useColor && rendered.Length > 0) rendered += Ansi.Reset;

            var info = i < infoLines.Count ? infoLines[i] : string.Empty;
            var row = info.Length == 0 ? rendered : rendered + new string(' ', width - visible) + info;
            sb.Append(row.TrimEnd()).Append('\n');
        }

        return sb.ToString();
    }
}
