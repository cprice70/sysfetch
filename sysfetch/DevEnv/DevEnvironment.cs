using System.Text.RegularExpressions;
using sysfetch.Helpers;

namespace sysfetch.DevEnv;

/// <summary>
/// Detects developer toolchains by reading install layouts on disk. A version
/// command is only run as a last resort, because each spawn costs 10-100 ms.
/// </summary>
public static partial class DevEnvironment
{
    public const string ModuleDotnet = "dotnet";
    public const string ModuleNode = "node";
    public const string ModulePython = "python";
    public const string ModuleAndroid = "android";
    public const string ModuleGit = "git";

    public static readonly string[] Modules = [ModuleDotnet, ModuleNode, ModulePython, ModuleAndroid, ModuleGit];

    /// <summary>Runs the requested detectors in parallel under a shared deadline.</summary>
    public static DevInfo Collect(IReadOnlyCollection<string> modules, TimeSpan timeout, string workingDirectory)
    {
        var info = new DevInfo();
        Func<string, string?> env = Environment.GetEnvironmentVariable;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        bool Wants(string m) => modules.Contains(m, StringComparer.OrdinalIgnoreCase);
        Task<T?> Run<T>(string module, Func<T?> probe) where T : struct =>
            Wants(module) ? Probe.Start(probe) : Task.FromResult<T?>(null);

        var dotnet = Run(ModuleDotnet, () => DetectDotnet(env));
        var node = Wants(ModuleNode) ? Probe.Start(() => DetectNode(env)) : Task.FromResult<string?>(null);
        var python = Run(ModulePython, () => DetectPython(env, home, workingDirectory));
        var android = Run(ModuleAndroid, () => DetectAndroid(env, home));
        var git = Run(ModuleGit, () => FindGit(workingDirectory));

        Probe.WaitAll(timeout, dotnet, node, python, android, git);

        if (Probe.Result(dotnet, null) is { } d) (info.DotnetSdk, info.DotnetSdkCount) = d;
        info.Node = Probe.Result(node, null);
        if (Probe.Result(python, null) is { } p) (info.Python, info.PythonSource) = p;
        if (Probe.Result(android, null) is { } a) (info.AndroidApi, info.AndroidBuildTools) = a;
        if (Probe.Result(git, null) is { } g) (info.GitRepo, info.GitBranch) = g;

        return info;
    }

    // ---------- PATH ----------

    /// <summary>Finds an executable on PATH and resolves symlinks to the real file.</summary>
    public static string? ResolveOnPath(string command, string? pathEnv, bool isWindows)
    {
        if (string.IsNullOrEmpty(pathEnv)) return null;
        var extensions = isWindows ? new[] { ".exe", ".cmd", ".bat" } : new[] { string.Empty };

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, command + ext);
                if (!File.Exists(candidate)) continue;
                try
                {
                    var target = File.ResolveLinkTarget(candidate, returnFinalTarget: true);
                    return target?.FullName ?? Path.GetFullPath(candidate);
                }
                catch (IOException)
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        return null;
    }

    // ---------- .NET ----------

    private static (string, int)? DetectDotnet(Func<string, string?> env)
    {
        var roots = new List<string>();
        if (env("DOTNET_ROOT") is { Length: > 0 } dotnetRoot) roots.Add(dotnetRoot);
        if (ResolveOnPath("dotnet", env("PATH"), OperatingSystem.IsWindows()) is { } exe) roots.Add(Path.GetDirectoryName(exe)!);
        roots.AddRange(OperatingSystem.IsWindows()
            ? [Path.Combine(env("ProgramFiles") ?? @"C:\Program Files", "dotnet")]
            : ["/usr/local/share/dotnet", "/usr/share/dotnet", "/usr/lib/dotnet", Path.Combine(env("HOME") ?? "", ".dotnet")]);

        foreach (var root in roots)
        {
            var sdks = GetDotnetSdks(root);
            if (sdks.Count > 0) return (sdks[^1], sdks.Count);
        }

        return null;
    }

    /// <summary>Installed SDK versions under {root}/sdk, sorted ascending.</summary>
    public static List<string> GetDotnetSdks(string dotnetRoot)
    {
        var sdkDir = Path.Combine(dotnetRoot, "sdk");
        if (!Directory.Exists(sdkDir)) return [];

        return Directory.EnumerateDirectories(sdkDir)
            .Select(Path.GetFileName)
            .Where(name => name is not null && char.IsAsciiDigit(name[0]) && File.Exists(Path.Combine(sdkDir, name, "dotnet.dll")))
            .Select(name => name!)
            .Order(VersionComparer.Instance)
            .ToList();
    }

    // ---------- Node ----------

    private static string? DetectNode(Func<string, string?> env)
    {
        var node = ResolveOnPath("node", env("PATH"), OperatingSystem.IsWindows());
        if (node is null) return null;

        var fromInstall = NodeVersionFromInstall(node);
        if (fromInstall is not null) return fromInstall;

        var output = CommandHelpers.OutputFromCommand(node, "--version").Trim().TrimStart('v');
        return output.Length > 0 ? output : null;
    }

    /// <summary>
    /// Reads the version from include/node/node_version.h next to the binary, or from a
    /// versioned install path (nvm, Homebrew Cellar, fnm, volta).
    /// </summary>
    public static string? NodeVersionFromInstall(string nodeBinary)
    {
        var binDir = Path.GetDirectoryName(nodeBinary);
        if (binDir is not null)
        {
            var header = Path.Combine(binDir, "..", "include", "node", "node_version.h");
            if (File.Exists(header))
            {
                var text = File.ReadAllText(header);
                string? major = null, minor = null, patch = null;
                foreach (Match m in NodeVersionDefine().Matches(text))
                {
                    switch (m.Groups[1].Value)
                    {
                        case "MAJOR": major = m.Groups[2].Value; break;
                        case "MINOR": minor = m.Groups[2].Value; break;
                        case "PATCH": patch = m.Groups[2].Value; break;
                    }
                }
                if (major is not null && minor is not null && patch is not null) return $"{major}.{minor}.{patch}";
            }
        }

        return VersionInPath().Match(nodeBinary.Replace('\\', '/')) is { Success: true } match ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"#define\s+NODE_(MAJOR|MINOR|PATCH)_VERSION\s+(\d+)")]
    private static partial Regex NodeVersionDefine();

    [GeneratedRegex("#define\\s+PY_VERSION\\s+\"([^\"]+)\"")]
    private static partial Regex PyVersionDefine();

    [GeneratedRegex(@"/v?(\d+\.\d+\.\d+)(?:_\d+)?/")]
    private static partial Regex VersionInPath();

    // ---------- Python ----------

    private static (string, string?)? DetectPython(Func<string, string?> env, string home, string workingDirectory)
    {
        if (env("VIRTUAL_ENV") is { Length: > 0 } venv && PythonVersionFromVenv(venv) is { } venvVersion)
            return (venvVersion, "venv");

        var python = ResolveOnPath("python3", env("PATH"), OperatingSystem.IsWindows())
                     ?? ResolveOnPath("python", env("PATH"), OperatingSystem.IsWindows());
        if (python is null) return null;

        var normalized = python.Replace('\\', '/');
        if (normalized.Contains("/.pyenv/shims/") || normalized.Contains("/pyenv-win/shims/"))
        {
            var pyenvRoot = env("PYENV_ROOT") is { Length: > 0 } r ? r : Path.Combine(home, ".pyenv");
            var version = PyenvVersion(env("PYENV_VERSION"), workingDirectory, pyenvRoot);
            if (version is not null && version != "system") return (version, "pyenv");
        }

        if (PythonVersionFromInstall(python) is { } installVersion) return (installVersion, null);

        // macOS /usr/bin/python3 is a stub that can prompt to install the Command Line Tools; never run it.
        if (OperatingSystem.IsMacOS() && normalized == "/usr/bin/python3") return null;

        var output = CommandHelpers.OutputFromCommand(python, "--version").Trim();
        return output.StartsWith("Python ", StringComparison.Ordinal) ? (output["Python ".Length..], null) : null;
    }

    /// <summary>pyenv resolution order: PYENV_VERSION, .python-version up the tree, then $PYENV_ROOT/version.</summary>
    public static string? PyenvVersion(string? pyenvVersionEnv, string workingDirectory, string pyenvRoot)
    {
        if (!string.IsNullOrWhiteSpace(pyenvVersionEnv)) return pyenvVersionEnv.Split(':')[0].Trim();

        for (var dir = new DirectoryInfo(workingDirectory); dir is not null; dir = dir.Parent)
        {
            var file = Path.Combine(dir.FullName, ".python-version");
            if (File.Exists(file)) return FirstToken(File.ReadAllText(file));
        }

        var global = Path.Combine(pyenvRoot, "version");
        return File.Exists(global) ? FirstToken(File.ReadAllText(global)) : null;
    }

    public static string? PythonVersionFromVenv(string venvDir)
    {
        var cfg = Path.Combine(venvDir, "pyvenv.cfg");
        if (!File.Exists(cfg)) return null;

        foreach (var line in File.ReadLines(cfg))
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2 && parts[0].Trim() is "version" or "version_info")
                return parts[1].Trim();
        }
        return null;
    }

    /// <summary>Reads PY_VERSION from include/python3.X/patchlevel.h beside the interpreter.</summary>
    public static string? PythonVersionFromInstall(string pythonBinary)
    {
        var binDir = Path.GetDirectoryName(pythonBinary);
        if (binDir is null) return null;

        var include = Path.Combine(binDir, "..", "include");
        if (Directory.Exists(include))
        {
            foreach (var dir in Directory.EnumerateDirectories(include, "python3*").Order(StringComparer.Ordinal).Reverse())
            {
                var header = Path.Combine(dir, "patchlevel.h");
                if (!File.Exists(header)) continue;
                var match = PyVersionDefine().Match(File.ReadAllText(header));
                if (match.Success) return match.Groups[1].Value;
            }
        }

        // e.g. /opt/homebrew/Cellar/python@3.12/3.12.4/... or .../bin/python3.12
        return VersionInPath().Match(pythonBinary.Replace('\\', '/')) is { Success: true } m ? m.Groups[1].Value : null;
    }

    // ---------- Android ----------

    private static (string?, string?)? DetectAndroid(Func<string, string?> env, string home)
    {
        var candidates = new List<string?> { env("ANDROID_HOME"), env("ANDROID_SDK_ROOT") };
        if (OperatingSystem.IsMacOS()) candidates.Add(Path.Combine(home, "Library", "Android", "sdk"));
        else if (OperatingSystem.IsWindows()) candidates.Add(Path.Combine(env("LOCALAPPDATA") ?? "", "Android", "Sdk"));
        else candidates.Add(Path.Combine(home, "Android", "Sdk"));

        foreach (var root in candidates)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
            var result = ReadAndroidSdk(root);
            if (result.api is not null || result.buildTools is not null) return result;
        }

        return null;
    }

    /// <summary>Highest installed platform API level and build-tools version.</summary>
    public static (string? api, string? buildTools) ReadAndroidSdk(string sdkRoot)
    {
        string? api = null;
        var platforms = Path.Combine(sdkRoot, "platforms");
        if (Directory.Exists(platforms))
        {
            api = Directory.EnumerateDirectories(platforms)
                .Select(Path.GetFileName)
                .Select(name => name!.StartsWith("android-", StringComparison.Ordinal) ? name["android-".Length..] : null)
                .Where(level => level is not null)
                .Order(VersionComparer.Instance)
                .LastOrDefault();
        }

        string? buildTools = null;
        var tools = Path.Combine(sdkRoot, "build-tools");
        if (Directory.Exists(tools))
        {
            buildTools = Directory.EnumerateDirectories(tools)
                .Select(Path.GetFileName)
                .Where(name => name is not null && char.IsAsciiDigit(name[0]))
                .Order(VersionComparer.Instance)
                .LastOrDefault();
        }

        return (api, buildTools);
    }

    // ---------- Git ----------

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> to the nearest .git and reads
    /// HEAD directly. Supports linked worktrees and submodules (".git" file).
    /// </summary>
    public static (string repo, string? branch)? FindGit(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var dotGit = Path.Combine(dir.FullName, ".git");
            string? gitDir = null;

            if (Directory.Exists(dotGit))
            {
                gitDir = dotGit;
            }
            else if (File.Exists(dotGit))
            {
                var content = File.ReadAllText(dotGit).Trim();
                if (content.StartsWith("gitdir:", StringComparison.Ordinal))
                {
                    var path = content["gitdir:".Length..].Trim();
                    gitDir = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(dir.FullName, path));
                }
            }

            if (gitDir is null) continue;

            var repo = RepoName(dir.FullName, gitDir);
            var head = Path.Combine(gitDir, "HEAD");
            return (repo, File.Exists(head) ? ParseHead(File.ReadAllText(head)) : null);
        }

        return null;
    }

    public static string? ParseHead(string head)
    {
        head = head.Trim();
        const string refPrefix = "ref:";
        if (head.StartsWith(refPrefix, StringComparison.Ordinal))
        {
            var reference = head[refPrefix.Length..].Trim();
            return reference.StartsWith("refs/heads/", StringComparison.Ordinal) ? reference["refs/heads/".Length..] : reference;
        }

        return head.Length >= 7 && head.All(char.IsAsciiHexDigit) ? $"detached {head[..7]}" : null;
    }

    private static string RepoName(string workTree, string gitDir)
    {
        // Linked worktree: <repo>/.git/worktrees/<name> -> report the main repo's name.
        var normalized = gitDir.Replace('\\', '/');
        var marker = normalized.IndexOf("/.git/worktrees/", StringComparison.Ordinal);
        if (marker > 0) return Path.GetFileName(normalized[..marker]);
        return Path.GetFileName(workTree.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static string? FirstToken(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
}

/// <summary>Orders dotted versions numerically; a pre-release sorts before its release.</summary>
public sealed class VersionComparer : IComparer<string?>
{
    public static readonly VersionComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var (xCore, xPre) = Split(x);
        var (yCore, yPre) = Split(y);

        var xParts = xCore.Split('.');
        var yParts = yCore.Split('.');
        for (var i = 0; i < Math.Max(xParts.Length, yParts.Length); i++)
        {
            var xs = i < xParts.Length ? xParts[i] : "0";
            var ys = i < yParts.Length ? yParts[i] : "0";
            var bothNumeric = long.TryParse(xs, out var xn) & long.TryParse(ys, out var yn);
            var c = bothNumeric ? xn.CompareTo(yn) : string.CompareOrdinal(xs, ys);
            if (c != 0) return c;
        }

        if (xPre is null) return yPre is null ? 0 : 1;
        if (yPre is null) return -1;
        return string.CompareOrdinal(xPre, yPre);
    }

    private static (string core, string? pre) Split(string version)
    {
        var dash = version.IndexOf('-');
        return dash < 0 ? (version, null) : (version[..dash], version[(dash + 1)..]);
    }
}
