using System.Diagnostics;
using System.Text.RegularExpressions;
using sysfetch.Extensions;

namespace sysfetch.Linux
{
    public class InfoFetch : IInfoFetch
    {
        public SysInfo SysInfo { get; } = new();

        private const string CpuPath = "/proc/cpuinfo";

        public InfoFetch()
        {
            SysInfo.OSName = "Linux";
            SysInfo.Kernel = GetDistro("off");
            SysInfo.CPU = GetCPUs();
            SysInfo.Model = GetModel();
            SysInfo.Uptime = GetUptime();
            SysInfo.GPUs = GetGPUs();
            SysInfo.Memory = GetMemory();
        }

        private static string GetDistro(string distroShorthand)
        {
            string distro = "";
            if (CommandHelpers.IsCommandAvailable("lsb_release"))
            {
                distro = distroShorthand switch
                {
                    "on" => CommandHelpers.OutputFromCommand("lsb_release", "-si"),
                    "tiny" => CommandHelpers.OutputFromCommand("lsb_release", "-si"),
                    _ => CommandHelpers.OutputFromCommand("lsb_release", "-sd")
                };
            }
            else if (File.Exists("/etc/os-release") ||
                     File.Exists("/usr/lib/os-release") ||
                     File.Exists("/etc/openwrt_release") ||
                     File.Exists("/etc/lsb-release"))
            {
                string[] files =
                    { "/etc/lsb-release", "/usr/lib/os-release", "/etc/os-release", "/etc/openwrt_release" };
                foreach (var file in files)
                {
                    if (!File.Exists(file)) continue;
                    string[] lines = File.ReadAllLines(file);
                    Dictionary<string, string> values =
                        lines.Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);

                    switch (distroShorthand)
                    {
                        case "on":
                            distro = values.ContainsKey("NAME")
                                ? values["NAME"]
                                : values["DISTRIB_ID"] + " " + (values.ContainsKey("VERSION_ID")
                                    ? values["VERSION_ID"]
                                    : values["DISTRIB_RELEASE"]);
                            break;
                        case "tiny":
                            distro = values.ContainsKey("NAME") ? values["NAME"] : values["DISTRIB_ID"];
                            if (!values.ContainsKey("NAME"))
                            {
                                distro = values.ContainsKey("TAILS_PRODUCT_NAME")
                                    ? values["TAILS_PRODUCT_NAME"]
                                    : distro;
                            }

                            break;
                        case "off":
                            distro = values.ContainsKey("PRETTY_NAME")
                                ? values["PRETTY_NAME"]
                                : values["DISTRIB_DESCRIPTION"];
                            if (values.ContainsKey("UBUNTU_CODENAME"))
                            {
                                distro += " " + values["UBUNTU_CODENAME"];
                            }

                            break;
                    }

                    break;
                }
            }
            return distro.RemoveNewlines();
        }

        private static IEnumerable<string?> GetCPUs()
            {
                if (!File.Exists(CpuPath)) yield return null;

                var cpuInfo = File.ReadAllText(CpuPath);

                var cpu = cpuInfo.GetValue("model name", ":");
                var cores = cpuInfo.GetValue("cpu cores", ":");
            
                yield return  $"{cpu} - {cores}";
            }

        //private const string GpuType = "all";

        private static IEnumerable<string> GetGPUs(string gpuType = "all")
        {
             // Read GPUs into array.
            string gpuOutput = string.Join("\n", CommandHelpers.OutputFromCommand("lspci", "-mm").Split('\n').Where(line => line.Contains("Display") || line.Contains("3D") || line.Contains("VGA")));
            string[] gpus = gpuOutput.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

            // Remove duplicate Intel Graphics outputs.
            // This fixes cases where the outputs are both
            // Intel but not entirely identical.
            //
            // Checking the first two array elements should
            // be safe since there won't be 2 intel outputs if
            // there's a dedicated GPU in play.
            if (gpus.Length > 1 && gpus[0].Contains("Intel") && gpus[1].Contains("Intel"))
            {
                gpus = gpus.Skip(1).ToArray();
            }

            foreach (string gpu in gpus)
            {
                var gputemp = gpu;
                // GPU shorthand tests.
                if ((gpuType == "dedicated" && gpu.Contains("Intel")) || (gpuType == "integrated" && !gpu.Contains("Intel")))
                {
                    continue;
                }

                string brand = "";
                if (gpu.Contains("Advanced"))
                {
                    brand = gpu.Contains("AMD") || gpu.Contains("ATI") ? "AMD ATI" : gpu.Contains("AMD") ? "AMD" : "ATi";
                    gputemp = Regex.Replace(gputemp, @"\[AMD\/ATI\]|\[AMD\]|OEM|Advanced Micro Devices, Inc.", "");
                    gputemp = Regex.Replace(gputemp, @"\[|\]", "");
                    gputemp = brand + " " + gputemp;
                }
                else if (gpu.Contains("Intel") || gpu.Contains("NVIDIA"))
                {
                    string pattern = "\"[^\"]*\"";
                    MatchCollection matches = Regex.Matches(gputemp, pattern);
                    gputemp = matches[matches.Count - 1].Value.Trim('"');

                    if (gpu.Contains("NVIDIA"))
                    {
                        string pattern2 = @"\[(.*?)\]";
                        Match match = Regex.Match(gputemp, pattern2);
                        gputemp = "NVIDIA " + match.Value.Trim('[', ']');
                    }
                    else if (gpu.Contains("Intel"))
                    {
                        gputemp = "Intel " + gputemp;
                    }
                }
                else if (gputemp.Contains("MCST"))
                {
                    gputemp = "MCST MGA2";
                }
                else if (gputemp.Contains("VirtualBox"))
                {
                    gputemp = "VirtualBox Graphics Adapter";
                }
                else if (gpuType == "off")
                {
                    gputemp = Regex.Replace(gputemp, "AMD|NVIDIA|Intel", "");
                }
                else
                {
                    continue;
                }
                
                yield return gputemp;
            }
        }

        private static string GetModel()
        {
            if (Directory.Exists("/system/app/") && Directory.Exists("/system/priv-app"))
            {
                return Environment.GetEnvironmentVariable("ro.product.brand") + " " + Environment.GetEnvironmentVariable("ro.product.model");
            }
            else if (File.Exists("/sys/devices/virtual/dmi/id/board_vendor") || File.Exists("/sys/devices/virtual/dmi/id/board_name"))
            {
                var result = File.ReadAllText("/sys/devices/virtual/dmi/id/board_vendor");
                result += " " + File.ReadAllText("/sys/devices/virtual/dmi/id/board_name");
                result = result.RemoveNewlines();
                return result;
            }
            else if (File.Exists("/sys/devices/virtual/dmi/id/product_name") || File.Exists("/sys/devices/virtual/dmi/id/product_version"))
            {
                
                var result = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name");
                result += " " + File.ReadAllText("/sys/devices/virtual/dmi/id/product_version");
                return result;
            }
            else if (File.Exists("/sys/firmware/devicetree/base/model"))
            {
                return File.ReadAllText("/sys/firmware/devicetree/base/model");
            }
            else if (File.Exists("/tmp/sysinfo/model"))
            {
                return File.ReadAllText("/tmp/sysinfo/model");
            }

            return string.Empty;
        }

        private string _uptime = string.Empty;
        private string GetUptime()
        {
            if (File.Exists("/proc/uptime"))
            {
                _uptime = File.ReadAllText("/proc/uptime");
                _uptime = _uptime.Split('.')[0];
                return Convert.ToInt32(_uptime).GetHumanReadableString();
            }

            var bootTime = DateTime.Parse(System.Diagnostics.Process.Start("uptime -s").StandardOutput.ReadToEnd().Trim());
            var now = DateTime.Now;
            return Convert.ToInt32((now - bootTime).TotalSeconds).GetHumanReadableString();
        }

        private static string GetMemory()
        {
            var memUsed = 0;
            
            // MemUsed = Memtotal + Shmem - MemFree - Buffers - Cached - SReclaimable
            // Source: https://github.com/KittyKatt/screenFetch/issues/386#issuecomment-249312716
            var memInfo = File.ReadAllText("/proc/meminfo");
            memInfo = memInfo.Replace("kB", "");
            
            var memTotal = Convert.ToInt32(memInfo.GetValue("MemTotal", ":"));
            var shMem = Convert.ToInt32(memInfo.GetValue("Shmem", ":"));
            var memFree = Convert.ToInt32(memInfo.GetValue("MemFree", ":"));
            var buffers = Convert.ToInt32(memInfo.GetValue("Buffers", ":"));
            var cached = Convert.ToInt32(memInfo.GetValue("Cached", ":"));
            var sReclaimable = Convert.ToInt32(memInfo.GetValue("SReclaimable", ":"));

            if (!memInfo.Contains("MemAvailable"))
            {
                memUsed = Convert.ToInt32(memInfo.GetValue("MemAvailable", ":"));
                memUsed /= 1024;
            }
            else
            {
                memUsed = memTotal + shMem - memFree - buffers - cached - sReclaimable;
                memUsed /= 1024;
            }

            return $"{memUsed} MB / {memTotal} MB";
        }

        private IEnumerable<string> GetResolutions(string refreshRate = "off")
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
                        string output = CommandHelpers.OutputFromCommand("xrandr", "--nograb --current");
                        string[] parts = output.Split(new string[] { "connected ", " +", "(" }, StringSplitOptions.None);
                        string[] resolutions = parts.Where(p => p.Contains("x")).ToArray();
                        resolution = string.Join(", ", resolutions);
                        resolution = resolution.Replace("primary, ", "");
                        resolution = resolution.Replace("primary ", "");
                        yield return resolution;
                        break;
                    }
                }
                resolution = resolution.Replace("*", "");
            }
            else if (CommandHelpers.IsCommandAvailable("xwininfo") && !string.IsNullOrEmpty(display) && string.IsNullOrEmpty(waylandDisplay))
            {
                string output = CommandHelpers.OutputFromCommand("xwininfo", "-root");
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
                string output =  CommandHelpers.OutputFromCommand("xdpyinfo", "");
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
                foreach (string directory in directories)
                {
                    var filePath = Path.Combine(directory, "modes");
                    if (!File.Exists(filePath)) continue;
                    string? singleResolution = File.ReadLines(filePath).FirstOrDefault();
                    if (!string.IsNullOrEmpty(singleResolution))
                    {
                        yield return singleResolution;
                    }
                }
            }
        }
    }
}