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
            SysInfo.CPU = GetCPUs();
            SysInfo.Model = GetModel();
            SysInfo.Uptime = GetUptime();
            SysInfo.GPUs = GetGPUs();
            SysInfo.Memory = GetMemory();
        }

        private static IEnumerable<string?> GetCPUs()
        {
            if (!File.Exists(CpuPath)) yield return null;

            var cpuInfo = File.ReadAllText(CpuPath);

            var cpu = cpuInfo.GetValue("Model Name");
            var cores = cpuInfo.GetValue("cpu cores");
            
            yield return  $"{cpu} - {cores}";
        }

        private const string GpuType = "all";

        private static IEnumerable<string> GetGPUs()
        {
            // Read GPUs into array.
            var gpuCmd = CommandHelpers.OutputFromCommand("lspci","-mm");
            var gpu_cmd = string.Join('\n', Regex.Matches(gpuCmd, "\"Display|\"3D|\"VGA").OfType<Match>()
                .Select(m => m.Value)
                .Distinct()
                .Select(m => m.Replace("\"", "").Trim()));
            var gpus = gpu_cmd.Split('\n').ToList();
            
            if (gpus[0].Contains("Intel") && gpus[1].Contains("Intel"))
            {
                gpus.RemoveAt(0);
            }

            foreach (var gpu in gpus)
            {
                var gputemp = gpu;
                // GPU shorthand tests.
                if ((GpuType == "dedicated" && gputemp.Contains("Intel")) ||
                    (GpuType == "integrated" && !gputemp.Contains("Intel")))
                {
                    gpus.Remove(gputemp);
                    continue;
                }

                if (gputemp.Contains("Advanced"))
                {
                    var brand = gputemp.Replace("AMD*ATI", "AMD ATI");
                    brand = (brand.Length > 0) ? brand : gputemp.Replace("AMD", "AMD");
                    brand = (brand.Length > 0) ? brand : gputemp.Replace("ATI", "ATi");

                    gputemp = gputemp.Replace("[AMD/ATI] ", "");
                    gputemp = gputemp.Replace("[AMD] ", "");
                    gputemp = gputemp.Replace("OEM ", "");
                    gputemp = gputemp.Replace("Advanced Micro Devices, Inc.", "");
                    gputemp = gputemp.Replace("[", "");
                    gputemp = gputemp.Replace("]", "");
                    gputemp = brand + " " + gputemp;
                }
                else if (gputemp.Contains("NVIDIA"))
                {
                    gputemp = gputemp.Replace("[", "");
                    gputemp = gputemp.Replace("]", "");
                    gputemp = "NVIDIA " + gputemp;
                }
                else if (gputemp.Contains("Intel"))
                {
                    gputemp = gputemp.Replace("Intel", "Intel");
                    gputemp = gputemp.Replace("(R)", "");
                    gputemp = gputemp.Replace("Corporation", "");
                    gputemp = gputemp.Replace(" (", "");
                    gputemp = gputemp.Replace("Integrated Graphics Controller", "");
                    gputemp = gputemp.Replace("Xeon", "Intel HD Graphics");

                    if (gputemp.Trim().Length == 0)
                    {
                        gputemp = "Intel Integrated Graphics";
                    }
                }
                else if (gputemp.Contains("MCST"))
                {
                    gputemp = gputemp.Replace("MCST*MGA2", "MCST MGA2");
                }
                else if (gputemp.Contains("VirtualBox"))
                {
                    gputemp = "VirtualBox Graphics Adapter";
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
                    string singleResolution = File.ReadLines(filePath).FirstOrDefault();
                    if (!string.IsNullOrEmpty(singleResolution))
                    {
                        yield return singleResolution;
                        //resolution = singleResolution + ", " + resolution;
                    }
                }
            }
        }
    }
}