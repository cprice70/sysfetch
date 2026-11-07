using System.Reflection;
using sysfetch;
using sysfetch.Config;
using sysfetch.DevEnv;
using sysfetch.Output;

var cli = CommandLine.Parse(args);

switch (cli.Action)
{
    case CliAction.Error:
        Console.Error.WriteLine($"sysfetch: {cli.Error}");
        Console.Error.WriteLine("Try 'sysfetch --help'.");
        return 2;
    case CliAction.Help:
        Console.WriteLine(CommandLine.HelpText);
        return 0;
    case CliAction.Version:
        var version = typeof(SysInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        Console.WriteLine($"sysfetch {version.Split('+')[0]}");
        return 0;
    case CliAction.ListLogos:
        foreach (var logo in Logos.All) Console.WriteLine(logo.Name);
        return 0;
    case CliAction.GenerateConfig:
        var target = cli.ConfigPath ?? Settings.DefaultConfigPath(Environment.GetEnvironmentVariable, OperatingSystem.IsWindows());
        if (File.Exists(target))
        {
            Console.Error.WriteLine($"sysfetch: {target} already exists; not overwriting.");
            return 1;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
        File.WriteAllText(target, Settings.DefaultConfigJson());
        Console.WriteLine($"Wrote {target}");
        return 0;
}

var settings = new Settings();
var warnings = new List<string>();

if (!cli.NoConfig)
{
    var configPath = cli.ConfigPath ?? Settings.DefaultConfigPath(Environment.GetEnvironmentVariable, OperatingSystem.IsWindows());
    if (File.Exists(configPath))
        settings.ApplyJson(File.ReadAllText(configPath), warnings);
    else if (cli.ConfigPath is not null)
        warnings.Add($"config file not found: {configPath}");
}

if (cli.Json is true) settings.Json = true;
if (cli.Logo is not null) settings.Logo = cli.Logo;
if (cli.Color is not null && !settings.SetColorMode(cli.Color, warnings))
{
    Console.Error.WriteLine($"sysfetch: {warnings[^1]}");
    return 2;
}

foreach (var warning in warnings) Console.Error.WriteLine($"sysfetch: {warning}");

var timeout = TimeSpan.FromMilliseconds(settings.TimeoutMs);

// JSON always includes the dev section; text output only probes enabled modules.
var devModules = settings.Json ? DevEnvironment.Modules : settings.Modules.Intersect(DevEnvironment.Modules, StringComparer.OrdinalIgnoreCase).ToArray();
var devTask = devModules.Length > 0
    ? Task.Run(() => DevEnvironment.Collect(devModules, timeout, Environment.CurrentDirectory))
    : Task.FromResult(new DevInfo());
IInfoFetch? info = null;
if (OperatingSystem.IsWindows())
    info = new sysfetch.Windows.InfoFetch(timeout);
else if (OperatingSystem.IsLinux())
    info = new sysfetch.Linux.InfoFetch(timeout);
else if (OperatingSystem.IsMacOS())
    info = new sysfetch.MacOS.InfoFetch(timeout);

if (info is null)
{
    Console.Error.WriteLine("sysfetch: platform is not supported");
    return 1;
}

info.SysInfo.Dev = devTask.Wait(timeout) && devTask.IsCompletedSuccessfully ? devTask.Result : new DevInfo();

var user = Environment.UserName;
var hostName = Environment.MachineName;

if (settings.Json)
{
    Console.Out.Write(JsonRenderer.Render(info.SysInfo, user, hostName));
    return 0;
}

var useColor = Ansi.ShouldUseColor(settings.Color, Console.IsOutputRedirected, Environment.GetEnvironmentVariable);
Console.Out.Write(TextRenderer.Render(info.SysInfo, settings, useColor, user, hostName));
return 0;
