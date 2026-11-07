using System.Text.RegularExpressions;
using sysfetch.Extensions;
using sysfetch.Helpers;
using sysfetch.Linux.Helpers;

namespace sysfetch.Linux
{
    public class InfoFetch : IInfoFetch
    {
        public SysInfo SysInfo { get; } = new();

        private const string CpuPath = "/proc/cpuinfo";

        public InfoFetch() : this(Probe.DefaultTimeout) { }

        public InfoFetch(TimeSpan timeout)
        {
            var os = Probe.Start(() => GetDistro("off"));
            var osId = Probe.Start(GetOsId);
            var kernel = Probe.Start(GetKernel);
            var cpu = Probe.Start(() => GetCPUs().ToList());
            var host = Probe.Start(GetModel);
            var uptime = Probe.Start(GetUptime);
            var gpus = Probe.Start(() => GetGPUs().ToList());
            var memory = Probe.Start(GetMemory);
            var resolutions = Probe.Start(() => GetResolutions().ToList());

            Probe.WaitAll(timeout, os, osId, kernel, cpu, host, uptime, gpus, memory, resolutions);

            SysInfo.OSName = Probe.Result(os, string.Empty);
            (SysInfo.OSId, SysInfo.OSIdLike) = Probe.Result(osId, ("linux", (IReadOnlyList<string>)[]));
            SysInfo.Kernel = Probe.Result(kernel, string.Empty);
            SysInfo.CPU = Probe.Result(cpu, []);
            SysInfo.Host = Probe.Result(host, string.Empty);
            SysInfo.Uptime = Probe.Result(uptime, string.Empty);
            SysInfo.GPUs = Probe.Result(gpus, []);
            SysInfo.Memory = Probe.Result(memory, string.Empty);
            SysInfo.Resolution = Probe.Result(resolutions, []);
        }

        private static string GetKernel()
        {
            var release = ReadFile("/proc/sys/kernel/osrelease");
            if (!string.IsNullOrEmpty(release)) return release;

            var uname = CommandHelpers.OutputFromCommand("uname", "-r").Trim();
            return uname;
        }

        private static string ReadFile(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path).Trim('\0', ' ', '\n', '\r', '\t') : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static (string id, IReadOnlyList<string> like) GetOsId()
        {
            var osRelease = ReadFile("/etc/os-release");
            if (string.IsNullOrEmpty(osRelease)) osRelease = ReadFile("/usr/lib/os-release");
            return LinuxHelpers.ParseOsReleaseId(osRelease);
        }

        private static string GetDistro(string distroShorthand)
        {
            string[] files = { "/etc/os-release", "/usr/lib/os-release", "/etc/lsb-release", "/etc/openwrt_release" };
            var fileInfo = files.Select(ReadFile).Where(f => !string.IsNullOrEmpty(f)).ToArray();

            var distro = fileInfo.Length > 0 ? LinuxHelpers.ParseDistroStrings(fileInfo, distroShorthand) : string.Empty;

            if (string.IsNullOrEmpty(distro) && CommandHelpers.IsCommandAvailable("lsb_release"))
            {
                distro = distroShorthand switch
                {
                    "on" or "tiny" => CommandHelpers.OutputFromCommand("lsb_release", "-si"),
                    _ => CommandHelpers.OutputFromCommand("lsb_release", "-sd")
                };
            }

            return distro.RemoveNewlines().Replace("\"", string.Empty);
        }

        private static IEnumerable<string?> GetCPUs()
        {
            var name = LinuxHardware.ParseCpuName(ReadFile(CpuPath));
            if (string.IsNullOrEmpty(name)) yield break;

            yield return $"{name} ({Environment.ProcessorCount})";
        }

        private static IEnumerable<string> GetGPUs(string gpuType = "all")
        {
            var gpus = LinuxHardware.GetGpusFromSysfs();
            if (gpus.Count > 0) return gpus;

            // Fallback for systems without readable sysfs PCI entries.
            if (!CommandHelpers.IsCommandAvailable("lspci")) return [];
            var gpuOutput = CommandHelpers.OutputFromCommand("lspci", "-mm");
            var devices = LinuxHelpers.ParseDeviceString(gpuOutput);
            return LinuxHelpers.ParseGPUStrings(devices.ToArray(), gpuType);
        }

        private static string GetModel()
        {
            const string dmi = "/sys/devices/virtual/dmi/id/";

            var product = $"{ReadFile(dmi + "product_name")} {ReadFile(dmi + "product_version")}".Trim();
            if (!string.IsNullOrEmpty(product) && !product.Contains("To be filled", StringComparison.OrdinalIgnoreCase))
                return product;

            var board = $"{ReadFile(dmi + "board_vendor")} {ReadFile(dmi + "board_name")}".Trim();
            if (!string.IsNullOrEmpty(board)) return board;

            var deviceTree = ReadFile("/sys/firmware/devicetree/base/model");
            if (!string.IsNullOrEmpty(deviceTree)) return deviceTree;

            return ReadFile("/tmp/sysinfo/model");
        }

        private static string GetUptime()
        {
            var uptime = ReadFile("/proc/uptime");
            var seconds = uptime.Split(' ', '.')[0];
            if (int.TryParse(seconds, out var value))
                return value.GetHumanReadableString();

            // Fallback: monotonic time since boot (excludes time spent suspended).
            return ((int)(Environment.TickCount64 / 1000)).GetHumanReadableString();
        }

        private static string GetMemory()
        {
            var memInfo = ReadFile("/proc/meminfo");
            if (string.IsNullOrEmpty(memInfo)) return string.Empty;

            return LinuxHelpers.ParseMemInfo(memInfo);
        }

        private static IEnumerable<string> GetResolutions(string refreshRate = "off")
        {
            var display = Environment.GetEnvironmentVariable("DISPLAY");
            var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
            var resolution = "";

            var drmModes = LinuxHardware.GetResolutionsFromDrm();
            if (drmModes.Count > 0)
            {
                foreach (var mode in drmModes) yield return mode;
                yield break;
            }

            if (CommandHelpers.IsCommandAvailable("xrandr") && !string.IsNullOrEmpty(display) && string.IsNullOrEmpty(waylandDisplay))
            {
                switch (refreshRate)
                {
                    case "on":
                    {
                        var output = CommandHelpers.OutputFromCommand("xrandr", "--nograb --current");
                        MatchCollection matches = Regex.Matches(output, @"[0-9]*\.[0-9]*\*");
                        resolution = string.Join(", ", matches.Select(m => m.Value + " @ " + m.Value + "Hz"));
                        yield return resolution;
                        break;
                    }
                    case "off":
                    {
                        var output = CommandHelpers.OutputFromCommand("xrandr", "--nograb --current");
                        var parts = output.Split(new string[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);
                        var resolutions = parts.Where(p => p.Contains(" connected")).ToArray();

                        foreach (var res in resolutions)
                        {
                            Regex regex = new Regex(@"\d+x\d+");
                            Match match = regex.Match(res);
                            if (match.Success)
                            {
                                yield return match.Value;
                            }
                        }
                        break;
                    }
                }
                resolution = resolution.Replace("*", "");
            }
            else if (CommandHelpers.IsCommandAvailable("xwininfo") && !string.IsNullOrEmpty(display) && string.IsNullOrEmpty(waylandDisplay))
            {
                var output = CommandHelpers.OutputFromCommand("xwininfo", "-root");
                Match widthMatch = Regex.Match(output, @"Width:[\s]+([0-9]+)");
                Match heightMatch = Regex.Match(output, @"Height:[\s]+([0-9]+)");
                if (widthMatch.Success && heightMatch.Success)
                {
                    string width = widthMatch.Groups[1].Value;
                    string height = heightMatch.Groups[1].Value;
                    resolution = width + "x" + height;
                    yield return resolution;
                }
            }
            else if (CommandHelpers.IsCommandAvailable("xdpyinfo") && !string.IsNullOrEmpty(display) && string.IsNullOrEmpty(waylandDisplay))
            {
                var output =  CommandHelpers.OutputFromCommand("xdpyinfo", "");
                Match resolutionMatch = Regex.Match(output, @"dimensions:[\s]+([0-9]+)x([0-9]+)");
                if (resolutionMatch.Success)
                {
                    string width = resolutionMatch.Groups[1].Value;
                    string height = resolutionMatch.Groups[2].Value;
                    resolution = width + "x" + height;
                    yield return resolution;
                }
            }
        }
    }
}