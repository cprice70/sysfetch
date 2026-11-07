using System.Runtime.Versioning;
using System.Text.Json;
using sysfetch.Extensions;
using sysfetch.Helpers;
using sysfetch.MacOS.Helpers;

namespace sysfetch.MacOS;

[SupportedOSPlatform("macos")]
public class InfoFetch : IInfoFetch
{
    public SysInfo SysInfo { get; } = new();

    public InfoFetch()
    {
        SysInfo.OSName = GetDistro("off");
        SysInfo.Kernel = GetKernel();
        SysInfo.CPU = GetCPUs("on");
        SysInfo.Host = Native.GetString("hw.model") ?? string.Empty;
        SysInfo.Uptime = GetUptime();
        SysInfo.Memory = GetMemory();

        var (gpus, resolutions) = GetDisplayInfo();
        SysInfo.GPUs = gpus;
        SysInfo.Resolution = resolutions;
    }

    private static string? GetDistro(string distroShorthand)
    {
        var version = Native.GetString("kern.osproductversion");
        var build = Native.GetString("kern.osversion");
        if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(build)) return null;
        return MacHelpers.GetDistroInfo(version, build, distroShorthand);
    }

    private static string GetKernel()
    {
        var type = Native.GetString("kern.ostype") ?? "Darwin";
        var release = Native.GetString("kern.osrelease");
        return string.IsNullOrEmpty(release) ? string.Empty : $"{type} {release}";
    }

    private static IEnumerable<string?> GetCPUs(string coreType)
    {
        var brand = Native.GetString("machdep.cpu.brand_string");
        if (string.IsNullOrEmpty(brand)) yield break;

        var cores = coreType == "physical"
            ? Native.GetInt64("hw.physicalcpu_max")
            : Native.GetInt64("hw.logicalcpu_max");

        yield return cores is null ? brand : $"{brand} ({cores})";
    }

    private static string GetUptime()
    {
        var boot = Native.GetTimeVal("kern.boottime");
        if (boot is null) return string.Empty;

        var seconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - boot.Value.Seconds;
        return ((int)seconds).GetHumanReadableString();
    }

    private static string GetMemory()
    {
        var total = Native.GetInt64("hw.memsize");
        var pageSize = Native.GetInt64("hw.pagesize");
        var stats = Native.GetVmStatistics();
        if (total is null || pageSize is null || stats is null) return string.Empty;

        // Matches Activity Monitor's "Memory Used": app memory + wired + compressed.
        var s = stats.Value;
        var appPages = (long)s.InternalPageCount - s.PurgeableCount;
        var usedBytes = (appPages + s.WireCount + s.CompressorPageCount) * pageSize.Value;

        return $"{usedBytes / 1024 / 1024} MiB / {total.Value / 1024 / 1024} MiB";
    }

    private static (List<string> gpus, List<string> resolutions) GetDisplayInfo()
    {
        var output = CommandHelpers.OutputFromCommand("system_profiler", "-json SPDisplaysDataType");
        return MacHelpers.ParseDisplays(output);
    }
}
