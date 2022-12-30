using System.Management;

namespace sysfetch.Windows
{
    public class InfoFetch : IInfoFetch
    {
        private string? _lastBootTime = string.Empty;
        private const string FMT = "O";
        private const string queryOS = "SELECT * FROM Win32_OperatingSystem";
        public SysInfo SysInfo { get; } = new();
        
        public InfoFetch()
        {

            SysInfo.Model = GetModel();
            var info = GetOSInfo();
            SysInfo.OSName = info.os;
            SysInfo.Kernel = info.kernel;
            _lastBootTime = info.lastBootTime;
            
            SysInfo.Uptime = GetUptime(_lastBootTime);
            SysInfo.CPU = GetCPUs();
            SysInfo.Resolution = GetResolutionList();
            SysInfo.GPUs = GetGPUs();
            SysInfo.Memory = GetMemory();
        }

        private static string? GetModel()
        {
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");

            using ManagementObjectCollection moc = mc.GetInstances();
            if (moc.Count != 0)
            {
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    // display general system information
                    return $"{mo["Manufacturer"]} {mo["Model"]}";
                }
            }

            return null;
        }

        private static (string os, string kernel, string lastBootTime) GetOSInfo()
        {
            using var mos = new ManagementObjectSearcher(queryOS);
            string osName = string.Empty, kernel = string.Empty, lastBootTime = string.Empty;
            
            foreach (ManagementObject managementObject in mos.Get())
            {
                if (managementObject["Caption"] != null)
                    osName = managementObject["Caption"].ToString();

                if (managementObject["Version"] != null)
                    kernel = managementObject["Version"].ToString();

                if (managementObject["LastBootUpTime"] != null)
                {
                    var boot = managementObject["LastBootUpTime"];
                    lastBootTime = boot.ToString();
                }
            }

            return (osName, kernel, lastBootTime);
        }
        
        public static string GetUptime(string lastBootTime)
        {
            var bootTime = ManagementDateTimeConverter.ToDateTime(lastBootTime);

            return (DateTime.Now - bootTime).ToString();
        }

        private static IEnumerable<string> GetResolutionList()
        {
            var mc2 = new ManagementClass("Win32_DesktopMonitor");
            
            using var moc2 = mc2.GetInstances();
            if (moc2.Count == 0) yield break;
            foreach (ManagementObject mo in mc2.GetInstances())
            {
                // display general system information
                if (mo["ScreenWidth"] is not null && mo["ScreenHeight"] is not null) 
                    yield return $"{mo["ScreenWidth"]}x{mo["ScreenHeight"]}";
            }
        }

        private const string queryCPUs = "SELECT * FROM Win32_Processor";
        private static IEnumerable<string?> GetCPUs()
        {
            using var mos_gpu = new ManagementObjectSearcher(queryCPUs); 
            foreach (ManagementObject managementObject in mos_gpu.Get())
            {
                if (managementObject["Name"] is not null)
                {
                    yield return managementObject["Name"].ToString();
                }
            }
        }

        private const string queryGPU = "SELECT * FROM Win32_VideoController";
        private static IEnumerable<string> GetGPUs()
        {
            var query = from ManagementObject mo in new ManagementObjectSearcher(queryGPU)
                    .Get()
                where mo["Name"] != null
                select mo["Name"].ToString();

            foreach (var name in query)
            {
                yield return name;
            }
        }
        
        private static string GetMemory()
        {
            ObjectQuery wql = new ObjectQuery(queryOS);
            using ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            using ManagementObjectCollection results = searcher.Get();

            foreach (ManagementObject result in results)
            {
                return
                    $"{(ulong)result["FreePhysicalMemory"] / 1024} MB / {(ulong)result["TotalVisibleMemorySize"] / 1024} MB";
            }

            return string.Empty;
        }
    }
}
