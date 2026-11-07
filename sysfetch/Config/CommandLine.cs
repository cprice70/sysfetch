namespace sysfetch.Config;

public enum CliAction
{
    Run,
    Help,
    Version,
    GenerateConfig,
    ListLogos,
    Error
}

public sealed class CommandLine
{
    public CliAction Action { get; private set; } = CliAction.Run;
    public string? ConfigPath { get; private set; }
    public bool NoConfig { get; private set; }
    public string? Error { get; private set; }

    // Overrides applied after the config file.
    public bool? Json { get; private set; }
    public string? Logo { get; private set; }
    public string? Color { get; private set; }

    public const string HelpText = """
        Usage: sysfetch [options]

          --json               Print system info as JSON
          --logo <name>        Logo to show: auto, none, apple, windows, linux,
                               arch, ubuntu, debian, fedora
          --no-logo            Same as --logo none
          --color <mode>       auto, always or never (auto honours NO_COLOR)
          --no-color           Same as --color never
          --config <path>      Read settings from this file
          --no-config          Ignore the config file
          --gen-config [path]  Write a default config file and exit
          --list-logos         List available logos
          -v, --version        Show version
          -h, --help           Show this help

        Config file: $XDG_CONFIG_HOME/sysfetch/config.json (or ~/.config/sysfetch/config.json,
        %APPDATA%\sysfetch\config.json on Windows).
        """;

    public static CommandLine Parse(IReadOnlyList<string> args)
    {
        var cli = new CommandLine();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            string? Next()
            {
                if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal)) return args[++i];
                return null;
            }

            switch (arg)
            {
                case "-h" or "--help":
                    cli.Action = CliAction.Help;
                    return cli;
                case "-v" or "--version":
                    cli.Action = CliAction.Version;
                    return cli;
                case "--list-logos":
                    cli.Action = CliAction.ListLogos;
                    return cli;
                case "--json":
                    cli.Json = true;
                    break;
                case "--no-logo":
                    cli.Logo = "none";
                    break;
                case "--no-color":
                    cli.Color = "never";
                    break;
                case "--no-config":
                    cli.NoConfig = true;
                    break;
                case "--gen-config":
                    cli.Action = CliAction.GenerateConfig;
                    cli.ConfigPath = Next() ?? cli.ConfigPath;
                    break;
                case "--logo" or "--color" or "--config":
                    var value = Next();
                    if (value is null) return Fail(cli, $"{arg} needs a value");
                    if (arg == "--logo") cli.Logo = value;
                    else if (arg == "--color") cli.Color = value;
                    else cli.ConfigPath = value;
                    break;
                default:
                    return Fail(cli, $"unknown option '{arg}'");
            }
        }

        return cli;
    }

    private static CommandLine Fail(CommandLine cli, string message)
    {
        cli.Action = CliAction.Error;
        cli.Error = message;
        return cli;
    }
}
