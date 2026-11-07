namespace sysfetch.Output;

/// <summary>
/// ASCII logo. Lines may contain color markers "$1".."$9" that switch to the
/// matching entry in <see cref="Colors"/>. <see cref="Colors"/>[0] is also the
/// accent color for info keys.
/// </summary>
public sealed record Logo(string Name, string[] Lines, string[] Colors)
{
    public int Width => Lines.Max(l => Logos.StripMarkers(l).Length);
}

public static class Logos
{
    public static readonly Logo Apple = new("apple",
    [
        "$1          .:",
        "$1         .;'",
        "$1   .-\"\"\"\"-.-\"\"\"\"-.",
        "$2  /               \\",
        "$3 |               .'",
        "$3 |              :",
        "$4 |               '.",
        "$5  \\               /",
        "$6   '-.._.-'-._.-'",
    ], ["green", "yellow", "brightred", "red", "magenta", "blue"]);

    public static readonly Logo Windows = new("windows",
    [
        "$1 ##########  ##########",
        "$1 ##########  ##########",
        "$1 ##########  ##########",
        "$1 ##########  ##########",
        "",
        "$1 ##########  ##########",
        "$1 ##########  ##########",
        "$1 ##########  ##########",
        "$1 ##########  ##########",
    ], ["brightblue"]);

    public static readonly Logo Linux = new("linux",
    [
        "$1      ___",
        "$1     ($2o o$1)",
        "$1    /( $3v$1 )\\",
        "$1   / |   | \\",
        "$1     |___|",
        "$3    _/   \\_",
    ], ["white", "brightblack", "yellow"]);

    public static readonly Logo Arch = new("arch",
    [
        "$1          /\\",
        "$1         /  \\",
        "$1        /    \\",
        "$1       /  /\\  \\",
        "$1      /  (  )  \\",
        "$1     /  /    \\  \\",
        "$1    /__/      \\__\\",
    ], ["cyan"]);

    public static readonly Logo Ubuntu = new("ubuntu",
    [
        "$1        _____",
        "$1     .-'  $2o$1  '-.",
        "$1    /  .-----.  \\",
        "$1   | $2o$1 |     | $2o$1 |",
        "$1    \\  '-----'  /",
        "$1     '-._____.-'",
    ], ["208", "white"]);

    public static readonly Logo Debian = new("debian",
    [
        "$1      _____",
        "$1     /  __ \\",
        "$1    |  /    |",
        "$1    |  \\___-",
        "$1     -_",
        "$1       --_",
    ], ["red"]);

    public static readonly Logo Fedora = new("fedora",
    [
        "$1       _____",
        "$1      /   __)$2\\",
        "$1      |  /  $2\\ \\",
        "$2   ___$1|  |$2__/ /",
        "$2  / ($1_    _$2)_/",
        "$2 / /  $1|  |",
        "$2 \\ \\__$1/  |",
        "$2  \\$1(_____/",
    ], ["blue", "white"]);

    public static IReadOnlyList<Logo> All { get; } = [Apple, Windows, Linux, Arch, Ubuntu, Debian, Fedora];

    private static readonly Dictionary<string, Logo> ById = new(StringComparer.OrdinalIgnoreCase)
    {
        ["macos"] = Apple,
        ["apple"] = Apple,
        ["windows"] = Windows,
        ["linux"] = Linux,
        ["arch"] = Arch,
        ["archarm"] = Arch,
        ["ubuntu"] = Ubuntu,
        ["debian"] = Debian,
        ["raspbian"] = Debian,
        ["fedora"] = Fedora,
    };

    /// <summary>Finds a logo by explicit name; null if unknown.</summary>
    public static Logo? ByName(string name) => ById.GetValueOrDefault(name);

    /// <summary>Picks the logo for an OS id, then its ID_LIKE parents, then generic Linux.</summary>
    public static Logo ForOs(string osId, IEnumerable<string> idLike)
    {
        if (ById.TryGetValue(osId, out var logo)) return logo;
        foreach (var like in idLike)
        {
            if (ById.TryGetValue(like, out logo)) return logo;
        }
        return Linux;
    }

    public static string StripMarkers(string line)
    {
        var chars = new System.Text.StringBuilder(line.Length);
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '$' && i + 1 < line.Length && char.IsAsciiDigit(line[i + 1])) { i++; continue; }
            chars.Append(line[i]);
        }
        return chars.ToString();
    }

    /// <summary>Replaces "$n" markers with ANSI codes, or strips them when color is off.</summary>
    public static string Colorize(string line, string[] colors, bool useColor)
    {
        if (!useColor) return StripMarkers(line);

        var sb = new System.Text.StringBuilder(line.Length + 16);
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '$' && i + 1 < line.Length && char.IsAsciiDigit(line[i + 1]))
            {
                var index = line[i + 1] - '1';
                if (index >= 0 && index < colors.Length) sb.Append(Ansi.Foreground(colors[index]));
                i++;
                continue;
            }
            sb.Append(line[i]);
        }
        return sb.ToString();
    }
}
