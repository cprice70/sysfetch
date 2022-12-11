using System.Management;
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
            GetCPUs();
            GetModel();
            GetUptime();
            SysInfo.GPUs = GetGPUs();
        }

        private void GetCPUs()
        {
            if (!File.Exists(CpuPath)) return;

            var cpuInfo = File.ReadAllText(CpuPath);

            var cpu = cpuInfo.GetValue("Model Name");
            var cores = cpuInfo.GetValue("cpu cores");
            var cpus = new List<string>() { $"{cpu} - {cores}" };

            SysInfo.CPU = cpus;
        }

        private const string GpuType = "all";

        private IEnumerable<string> GetGPUs()
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

        private void GetModel()
        {
            if (Directory.Exists("/system/app/") && Directory.Exists("/system/priv-app"))
            {
                SysInfo.Model = Environment.GetEnvironmentVariable("ro.product.brand") + " " + Environment.GetEnvironmentVariable("ro.product.model");
            }
            else if (File.Exists("/sys/devices/virtual/dmi/id/board_vendor") || File.Exists("/sys/devices/virtual/dmi/id/board_name"))
            {
                SysInfo.Model = File.ReadAllText("/sys/devices/virtual/dmi/id/board_vendor");
                SysInfo.Model += " " + File.ReadAllText("/sys/devices/virtual/dmi/id/board_name");
            }
            else if (File.Exists("/sys/devices/virtual/dmi/id/product_name") || File.Exists("/sys/devices/virtual/dmi/id/product_version"))
            {
                SysInfo.Model = File.ReadAllText("/sys/devices/virtual/dmi/id/product_name");
                SysInfo.Model += " " + File.ReadAllText("/sys/devices/virtual/dmi/id/product_version");
            }
            else if (File.Exists("/sys/firmware/devicetree/base/model"))
            {
                SysInfo.Model = File.ReadAllText("/sys/firmware/devicetree/base/model");
            }
            else if (File.Exists("/tmp/sysinfo/model"))
            {
                SysInfo.Model = File.ReadAllText("/tmp/sysinfo/model");
            }
        }

        private string _uptime = string.Empty;
        private void GetUptime()
        {
            if (File.Exists("/proc/uptime"))
            {
                _uptime = File.ReadAllText("/proc/uptime");
                _uptime = _uptime.Split('.')[0];
                SysInfo.Uptime = Convert.ToInt32(_uptime).GetHumanReadableString();
            }
            else
            {
                var bootTime = DateTime.Parse(System.Diagnostics.Process.Start("uptime -s").StandardOutput.ReadToEnd().Trim());
                var now = DateTime.Now;
                SysInfo.Uptime = Convert.ToInt32((now - bootTime).TotalSeconds).GetHumanReadableString();
            }
        }

        private void GetMemory()
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

            SysInfo.Memory = $"{memUsed} MB / {memTotal} MB";
        }
    }
}