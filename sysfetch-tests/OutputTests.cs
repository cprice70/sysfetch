using System.Text.Json;
using sysfetch;
using sysfetch.Config;
using sysfetch.Linux.Helpers;
using sysfetch.Output;

namespace sysfetch_tests;

public class OutputTests
{
    private static SysInfo Sample() => new()
    {
        OSName = "Ubuntu 24.04 LTS",
        OSId = "ubuntu",
        Host = "ThinkPad X1",
        Kernel = "6.8.0-45-generic",
        Uptime = "2 hours",
        CPU = ["Intel Core i7-1365U (12)"],
        GPUs = ["Intel Iris Xe Graphics"],
        Resolution = ["2560x1600"],
        Memory = "4096 MiB / 16000 MiB"
    };

    [TestCase("red", "\u001b[31m")]
    [TestCase("brightblue", "\u001b[94m")]
    [TestCase("208", "\u001b[38;5;208m")]
    [TestCase("nope", null)]
    [TestCase("256", null)]
    public void Ansi_Foreground(string name, string? expected)
    {
        Assert.That(Ansi.Foreground(name), Is.EqualTo(expected));
    }

    [Test]
    public void Ansi_VisibleLength_IgnoresEscapes()
    {
        Assert.That(Ansi.VisibleLength("\u001b[1m\u001b[31mOS\u001b[0m: x"), Is.EqualTo(5));
    }

    [TestCase("auto", false, null, null, true)]
    [TestCase("auto", true, null, null, false)]
    [TestCase("auto", false, "1", null, false)]
    [TestCase("auto", false, null, "dumb", false)]
    [TestCase("always", true, "1", null, true)]
    [TestCase("never", false, null, null, false)]
    public void Ansi_ShouldUseColor(string mode, bool redirected, string? noColor, string? term, bool expected)
    {
        string? Env(string key) => key switch { "NO_COLOR" => noColor, "TERM" => term, _ => null };
        Assert.That(Ansi.ShouldUseColor(mode, redirected, Env), Is.EqualTo(expected));
    }

    [TestCase("ubuntu", new string[0], "ubuntu")]
    [TestCase("linuxmint", new[] { "ubuntu", "debian" }, "ubuntu")]
    [TestCase("pop", new[] { "ubuntu", "debian" }, "ubuntu")]
    [TestCase("endeavouros", new[] { "arch" }, "arch")]
    [TestCase("gentoo", new string[0], "linux")]
    [TestCase("macos", new string[0], "apple")]
    [TestCase("windows", new string[0], "windows")]
    public void Logos_ForOs(string id, string[] like, string expected)
    {
        Assert.That(Logos.ForOs(id, like).Name, Is.EqualTo(expected));
    }

    [Test]
    public void Logos_AllHaveColorsForTheirMarkers()
    {
        foreach (var logo in Logos.All)
        {
            foreach (var line in logo.Lines)
            {
                for (var i = 0; i < line.Length - 1; i++)
                {
                    if (line[i] != '$' || !char.IsAsciiDigit(line[i + 1])) continue;
                    var index = line[i + 1] - '1';
                    Assert.That(index, Is.InRange(0, logo.Colors.Length - 1), $"{logo.Name}: bad marker in '{line}'");
                }
            }
            Assert.That(logo.Colors.All(Ansi.IsValidColor), Is.True, logo.Name);
        }
    }

    [Test]
    public void Logos_Colorize()
    {
        Assert.That(Logos.Colorize("$1ab$2c", ["red", "blue"], useColor: false), Is.EqualTo("abc"));
        Assert.That(Logos.Colorize("$1ab$2c", ["red", "blue"], useColor: true), Is.EqualTo("\u001b[31mab\u001b[34mc"));
    }

    [Test]
    public void TextRenderer_AlignsInfoBesideLogo()
    {
        var settings = new Settings { Logo = "arch" };
        var output = TextRenderer.Render(Sample(), settings, useColor: false, "me", "box");
        var lines = output.TrimEnd('\n').Split('\n');

        var column = Logos.Arch.Width + 3;
        Assert.That(lines[0][column..], Is.EqualTo("me@box"));
        Assert.That(lines[1][column..], Is.EqualTo("------"));
        Assert.That(lines[2][column..], Is.EqualTo("OS: Ubuntu 24.04 LTS"));
        Assert.That(lines.Any(l => l.EndsWith("Memory: 4096 MiB / 16000 MiB")), Is.True);
        Assert.That(output.Contains('\u001b'), Is.False, "no ANSI escapes when color is off");
        Assert.That(lines[^1], Is.Not.Empty, "trailing blank line should be trimmed");
    }

    [Test]
    public void TextRenderer_NoLogo_RespectsModuleOrderAndSeparator()
    {
        var settings = new Settings { Logo = "none", Separator = " -> ", Modules = ["memory", "os", "colors"] };
        var output = TextRenderer.Render(Sample(), settings, useColor: false, "me", "box");

        Assert.That(output, Is.EqualTo("Memory -> 4096 MiB / 16000 MiB\nOS -> Ubuntu 24.04 LTS\n"));
    }

    [Test]
    public void TextRenderer_SkipsEmptyValues()
    {
        var info = Sample();
        info.Host = string.Empty;
        info.GPUs = [];
        var output = TextRenderer.Render(info, new Settings { Logo = "none" }, useColor: false, "me", "box");

        Assert.That(output, Does.Not.Contain("Host"));
        Assert.That(output, Does.Not.Contain("GPU"));
    }

    [Test]
    public void JsonRenderer_ProducesExpectedShape()
    {
        var info = Sample();
        info.Host = "";
        var json = JsonRenderer.Render(info, "me", "box");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.That(root.GetProperty("os").GetString(), Is.EqualTo("Ubuntu 24.04 LTS"));
        Assert.That(root.GetProperty("host").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(root.GetProperty("cpus")[0].GetString(), Is.EqualTo("Intel Core i7-1365U (12)"));
        Assert.That(root.GetProperty("resolutions").GetArrayLength(), Is.EqualTo(1));
        Assert.That(root.GetProperty("user").GetString(), Is.EqualTo("me"));
    }

    [Test]
    public void ParseOsReleaseId_ReadsIdAndIdLike()
    {
        var (id, like) = LinuxHelpers.ParseOsReleaseId("NAME=\"Linux Mint\"\nID=linuxmint\nID_LIKE=\"ubuntu debian\"\n");

        Assert.That(id, Is.EqualTo("linuxmint"));
        Assert.That(like, Is.EqualTo(new[] { "ubuntu", "debian" }));
    }

    [Test]
    public void ParseOsReleaseId_DefaultsToLinux()
    {
        Assert.That(LinuxHelpers.ParseOsReleaseId(string.Empty).id, Is.EqualTo("linux"));
    }
}
