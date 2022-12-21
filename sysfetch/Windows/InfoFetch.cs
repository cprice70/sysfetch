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
            #region ComputerSystem
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");

            using ManagementObjectCollection moc = mc.GetInstances();
            if (moc.Count != 0)
            {
                foreach (ManagementObject mo in mc.GetInstances())
                {
                    // display general system information
                    SysInfo.Host = $"{mo["Manufacturer"]} {mo["Model"]}";
                    break;
                }
            }
            #endregion

            #region OperatingSystem
            using var mos = new ManagementObjectSearcher(queryOS);

            foreach (ManagementObject managementObject in mos.Get())
            {
                if (managementObject["Caption"] != null)
                    SysInfo.OSName = managementObject["Caption"].ToString();

                if (managementObject["Version"] != null)
                    SysInfo.Kernel = managementObject["Version"].ToString();

                if (managementObject["LastBootUpTime"] != null)
                {
                    var boot = managementObject["LastBootUpTime"];
                    _lastBootTime = boot.ToString();
                }
            }
            #endregion

            SysInfo.Uptime = GetUptime();
            SysInfo.CPU = GetCPUs();
            SysInfo.Resolution = GetResolutionList();
            SysInfo.GPUs = GetGPUs();
            SysInfo.Memory = GetMemory();
        }
        
        private string GetUptime()
        {
            var bootTime = ManagementDateTimeConverter.ToDateTime(_lastBootTime);

            return (DateTime.Now - bootTime).ToString();
        }

        private static IEnumerable<string> GetResolutionList()
        {
            var mc2 = new ManagementClass("Win32_DesktopMonitor");
            
            using ManagementObjectCollection moc2 = mc2.GetInstances();
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
