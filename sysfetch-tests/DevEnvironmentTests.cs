using sysfetch;
using sysfetch.Config;
using sysfetch.DevEnv;
using sysfetch.Output;

namespace sysfetch_tests;

public class DevEnvironmentTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("sysfetch-dev-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    private string Dir(params string[] parts) => Directory.CreateDirectory(Path.Combine([_root, .. parts])).FullName;

    private string WriteFile(string content, params string[] parts)
    {
        var path = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    [TestCase("10.0.300", "9.0.314", 1)]
    [TestCase("10.0.100", "10.0.100-rc.2.25502.107", 1)]
    [TestCase("10.0.100-preview.7", "10.0.100-rc.1", -1)]
    [TestCase("36", "VanillaIceCream", -1)]
    [TestCase("8.0.416", "8.0.416", 0)]
    public void VersionComparer_Orders(string x, string y, int expectedSign)
    {
        Assert.That(Math.Sign(VersionComparer.Instance.Compare(x, y)), Is.EqualTo(expectedSign));
    }

    [Test]
    public void GetDotnetSdks_SortsAndIgnoresIncompleteFolders()
    {
        foreach (var v in new[] { "9.0.307", "10.0.103", "8.0.421", "10.0.300" })
            WriteFile("", "dotnet", "sdk", v, "dotnet.dll");
        Dir("dotnet", "sdk", "NuGetFallbackFolder");
        Dir("dotnet", "sdk", "11.0.100"); // no dotnet.dll: partial install

        var sdks = DevEnvironment.GetDotnetSdks(Path.Combine(_root, "dotnet"));

        Assert.That(sdks, Is.EqualTo(new[] { "8.0.421", "9.0.307", "10.0.103", "10.0.300" }));
    }

    [Test]
    public void NodeVersionFromInstall_ReadsHeader()
    {
        var node = WriteFile("", "node", "bin", "node");
        WriteFile("#define NODE_MAJOR_VERSION 24\n#define NODE_MINOR_VERSION 14\n#define NODE_PATCH_VERSION 0\n",
            "node", "include", "node", "node_version.h");

        Assert.That(DevEnvironment.NodeVersionFromInstall(node), Is.EqualTo("24.14.0"));
    }

    [TestCase("/home/me/.nvm/versions/node/v22.11.0/bin/node", "22.11.0")]
    [TestCase("/opt/homebrew/Cellar/node@24/24.14.0_1/bin/node", "24.14.0")]
    [TestCase("/usr/bin/node", null)]
    public void NodeVersionFromInstall_FallsBackToPath(string path, string? expected)
    {
        Assert.That(DevEnvironment.NodeVersionFromInstall(path), Is.EqualTo(expected));
    }

    [Test]
    public void PyenvVersion_PrefersEnvThenLocalFileThenGlobal()
    {
        var project = Dir("work", "proj", "src");
        WriteFile("3.11.6\n", "work", "proj", ".python-version");
        WriteFile("3.12.8\n", "pyenv", "version");
        var pyenvRoot = Path.Combine(_root, "pyenv");

        Assert.That(DevEnvironment.PyenvVersion("3.13.0:3.12.8", project, pyenvRoot), Is.EqualTo("3.13.0"));
        Assert.That(DevEnvironment.PyenvVersion(null, project, pyenvRoot), Is.EqualTo("3.11.6"));
        Assert.That(DevEnvironment.PyenvVersion(null, Dir("elsewhere"), pyenvRoot), Is.EqualTo("3.12.8"));
    }

    [Test]
    public void PythonVersionFromVenv_ReadsPyvenvCfg()
    {
        WriteFile("home = /usr/bin\ninclude-system-site-packages = false\nversion = 3.12.3\n", "venv", "pyvenv.cfg");

        Assert.That(DevEnvironment.PythonVersionFromVenv(Path.Combine(_root, "venv")), Is.EqualTo("3.12.3"));
        Assert.That(DevEnvironment.PythonVersionFromVenv(Path.Combine(_root, "missing")), Is.Null);
    }

    [Test]
    public void PythonVersionFromInstall_ReadsPatchlevel()
    {
        var python = WriteFile("", "py", "bin", "python3.12");
        WriteFile("#define PY_VERSION              \"3.12.4\"\n", "py", "include", "python3.12", "patchlevel.h");

        Assert.That(DevEnvironment.PythonVersionFromInstall(python), Is.EqualTo("3.12.4"));
    }

    [Test]
    public void ReadAndroidSdk_PicksHighest()
    {
        foreach (var p in new[] { "android-34", "android-36", "android-35" }) Dir("sdk", "platforms", p);
        foreach (var b in new[] { "35.0.0", "36.1.0", "36.0.0" }) Dir("sdk", "build-tools", b);

        var (api, tools) = DevEnvironment.ReadAndroidSdk(Path.Combine(_root, "sdk"));

        Assert.That(api, Is.EqualTo("36"));
        Assert.That(tools, Is.EqualTo("36.1.0"));
    }

    [Test]
    public void FindGit_WalksUpToRepository()
    {
        WriteFile("ref: refs/heads/main\n", "myrepo", ".git", "HEAD");
        var nested = Dir("myrepo", "src", "deep");

        Assert.That(DevEnvironment.FindGit(nested), Is.EqualTo(("myrepo", "main")));
    }

    [Test]
    public void FindGit_LinkedWorktreeReportsMainRepo()
    {
        WriteFile("ref: refs/heads/feature/x\n", "main", ".git", "worktrees", "wt1", "HEAD");
        WriteFile($"gitdir: {Path.Combine(_root, "main", ".git", "worktrees", "wt1")}\n", "wt1", ".git");

        Assert.That(DevEnvironment.FindGit(Path.Combine(_root, "wt1")), Is.EqualTo(("main", "feature/x")));
    }

    [Test]
    public void FindGit_NoRepository()
    {
        Assert.That(DevEnvironment.FindGit(Dir("plain")), Is.Null);
    }

    [TestCase("ref: refs/heads/users/me/OM-1\n", "users/me/OM-1")]
    [TestCase("1f76483a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e\n", "detached 1f76483")]
    [TestCase("garbage", null)]
    public void ParseHead(string head, string? expected)
    {
        Assert.That(DevEnvironment.ParseHead(head), Is.EqualTo(expected));
    }

    [Test]
    public void DevInfo_Formatting()
    {
        var dev = new DevInfo
        {
            DotnetSdk = "10.0.300", DotnetSdkCount = 3,
            Python = "3.12.8", PythonSource = "pyenv",
            AndroidBuildTools = "36.1.0",
            GitRepo = "sysfetch", GitBranch = "main"
        };

        Assert.That(dev.FormatDotnet(), Is.EqualTo("SDK 10.0.300 (3 installed)"));
        Assert.That(dev.FormatPython(), Is.EqualTo("3.12.8 (pyenv)"));
        Assert.That(dev.FormatAndroid(), Is.EqualTo("build-tools 36.1.0"));
        Assert.That(dev.FormatGit(), Is.EqualTo("sysfetch @ main"));
        Assert.That(new DevInfo().FormatDotnet(), Is.Null);
    }

    [Test]
    public void TextRenderer_ShowsOnlyDetectedDevLines()
    {
        var info = new SysInfo { Dev = new DevInfo { Node = "24.14.0", GitRepo = "r", GitBranch = "b" } };
        var settings = new Settings { Logo = "none", Modules = ["dotnet", "node", "python", "android", "git"] };

        var output = TextRenderer.Render(info, settings, useColor: false, "me", "box");

        Assert.That(output, Is.EqualTo("Node: 24.14.0\nGit: r @ b\n"));
    }
}
