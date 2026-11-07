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

        public InfoFetch()
        {
            SysInfo.OSName = GetDistro("off");
            SysInfo.Kernel = GetKernel();
            SysInfo.CPU = GetCPUs();
            SysInfo.Host = GetModel();
            SysInfo.Uptime = GetUptime();
            SysInfo.GPUs = GetGPUs();
            SysInfo.Memory = GetMemory();
            SysInfo.Resolution = GetResolutions();
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
            if (!File.Exists(CpuPath)) yield break;

            var cpuInfo = File.ReadAllText(CpuPath);

            var cpu = cpuInfo.GetValue("model name", ":");
            var cores = cpuInfo.GetValue("cpu cores", ":");

            yield return string.IsNullOrEmpty(cores) ? cpu : $"{cpu} ({cores})";
        }
        
        private static IEnumerable<string> GetGPUs(string gpuType = "all")
        {
             // Read GPUs into array.
            var gpuOutput = CommandHelpers.OutputFromCommand("lspci", "-mm");
            var gpus = LinuxHelpers.ParseDeviceString(gpuOutput);
            
            return LinuxHelpers.ParseGPUStrings(gpus.ToArray(), gpuType);
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
            else if(Directory.Exists("/sys/class/drm"))
            {
                var directories = Directory.GetDirectories("/sys/class/drm");
                foreach (var directory in directories)
                {
                    var filePath = Path.Combine(directory, "modes");
                    if (!File.Exists(filePath)) continue;
                    var singleResolution = File.ReadLines(filePath).FirstOrDefault();
                    if (!string.IsNullOrEmpty(singleResolution))
                    {
                        yield return singleResolution;
                    }
                }
            }
        }
    }
}