using sysfetch.Config;

namespace sysfetch_tests;

public class ConfigTests
{
    [Test]
    public void ApplyJson_ReadsAllSettings()
    {
        var settings = new Settings();
        var warnings = new List<string>();

        settings.ApplyJson("""
            {
              // comments are allowed
              "logo": "fedora",
              "color": "never",
              "keyColor": "brightred",
              "separator": " = ",
              "timeoutMs": 250,
              "modules": ["os", "memory"],
            }
            """, warnings);

        Assert.That(warnings, Is.Empty);
        Assert.That(settings.Logo, Is.EqualTo("fedora"));
        Assert.That(settings.Color, Is.EqualTo("never"));
        Assert.That(settings.KeyColor, Is.EqualTo("brightred"));
        Assert.That(settings.Separator, Is.EqualTo(" = "));
        Assert.That(settings.TimeoutMs, Is.EqualTo(250));
        Assert.That(settings.Modules, Is.EqualTo(new[] { "os", "memory" }));
    }

    [Test]
    public void ApplyJson_WarnsOnBadValuesAndKeepsDefaults()
    {
        var settings = new Settings();
        var warnings = new List<string>();

        settings.ApplyJson("""{ "color": "rainbow", "keyColor": "chartreuse", "modules": ["os", "weather"], "timeoutMs": "fast" }""", warnings);

        Assert.That(settings.Color, Is.EqualTo("auto"));
        Assert.That(settings.KeyColor, Is.Null);
        Assert.That(settings.Modules, Is.EqualTo(new[] { "os" }));
        Assert.That(settings.TimeoutMs, Is.EqualTo(1000));
        Assert.That(warnings, Has.Count.EqualTo(4));
    }

    [Test]
    public void ApplyJson_InvalidJson()
    {
        var warnings = new List<string>();
        new Settings().ApplyJson("{ nope", warnings);
        Assert.That(warnings.Single(), Does.StartWith("config: invalid JSON"));
    }

    [Test]
    public void DefaultConfigJson_RoundTrips()
    {
        var settings = new Settings();
        var warnings = new List<string>();

        settings.ApplyJson(Settings.DefaultConfigJson(), warnings);

        Assert.That(warnings, Is.Empty);
        Assert.That(settings.Modules, Is.EqualTo(Settings.DefaultModules));
    }

    [Test]
    public void DefaultConfigPath_UsesXdgThenHome()
    {
        Assert.That(Settings.DefaultConfigPath(k => k == "XDG_CONFIG_HOME" ? "/x" : "/home/me", false),
            Is.EqualTo(Path.Combine("/x", "sysfetch", "config.json")));
        Assert.That(Settings.DefaultConfigPath(k => k == "HOME" ? "/home/me" : null, false),
            Is.EqualTo(Path.Combine("/home/me", ".config", "sysfetch", "config.json")));
        Assert.That(Settings.DefaultConfigPath(k => k == "APPDATA" ? "C:/AppData" : null, true),
            Is.EqualTo(Path.Combine("C:/AppData", "sysfetch", "config.json")));
    }

    [Test]
    public void CommandLine_ParsesOverrides()
    {
        var cli = CommandLine.Parse(["--json", "--logo", "arch", "--no-color", "--config", "/tmp/c.json"]);

        Assert.That(cli.Action, Is.EqualTo(CliAction.Run));
        Assert.That(cli.Json, Is.True);
        Assert.That(cli.Logo, Is.EqualTo("arch"));
        Assert.That(cli.Color, Is.EqualTo("never"));
        Assert.That(cli.ConfigPath, Is.EqualTo("/tmp/c.json"));
    }

    [TestCase(new[] { "--logo" }, "--logo needs a value")]
    [TestCase(new[] { "--logo", "--json" }, "--logo needs a value")]
    [TestCase(new[] { "--frobnicate" }, "unknown option '--frobnicate'")]
    public void CommandLine_Errors(string[] args, string expected)
    {
        var cli = CommandLine.Parse(args);

        Assert.That(cli.Action, Is.EqualTo(CliAction.Error));
        Assert.That(cli.Error, Is.EqualTo(expected));
    }

    [TestCase("-h", CliAction.Help)]
    [TestCase("--version", CliAction.Version)]
    [TestCase("--list-logos", CliAction.ListLogos)]
    [TestCase("--gen-config", CliAction.GenerateConfig)]
    public void CommandLine_Actions(string arg, CliAction expected)
    {
        Assert.That(CommandLine.Parse([arg]).Action, Is.EqualTo(expected));
    }
}
