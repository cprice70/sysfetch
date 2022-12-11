using System.Management;
using sysfetch.Extensions;

namespace sysfetch.Linux
{

    public class InfoFetch : IInfoFetch
    {
        public SysInfo SysInfo { get; } = new();

        private readonly string cpuPath = "/proc/cpuinfo";
        private void GetCPUs()
        {
            if (!File.Exists(cpuPath)) return;

            var cpuInfo = File.ReadAllText(cpuPath);

            var cpu = cpuInfo.GetValue("Model Name");
            var cores = cpuInfo.GetValue("cpu cores");
            var cpus = new List<string>() { $"{cpu} - {cores}" };

            SysInfo.CPU = cpus;
        }
    }

}