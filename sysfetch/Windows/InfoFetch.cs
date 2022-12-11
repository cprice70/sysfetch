using System.Management;

namespace sysfetch.Windows
{

    public class InfoFetch : IInfoFetch
    {
        private string? _lastBootTime = string.Empty;
        private const string FMT = "O";

        public SysInfo SysInfo { get; } = new();
        
        public InfoFetch()
        {
            #region ComputerSystem
            ManagementClass mc = new ManagementClass("Win32_ComputerSystem");

            ManagementObjectCollection moc = mc.GetInstances();
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
            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

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
            ManagementClass mc2 = new ManagementClass("Win32_DesktopMonitor");
            
            ManagementObjectCollection moc2 = mc2.GetInstances();
            if (moc2.Count == 0) yield break;
            foreach (ManagementObject mo in mc2.GetInstances())
            {
                // display general system information
                if (mo["ScreenWidth"] is not null && mo["ScreenHeight"] is not null) 
                    yield return $"{mo["ScreenWidth"]}x{mo["ScreenHeight"]}";
            }
        }

        private static IEnumerable<string>? GetCPUs()
        {
            ManagementObjectSearcher mos_gpu = new ManagementObjectSearcher("select * from Win32_Processor"); 
            foreach (ManagementObject managementObject in mos_gpu.Get())
            {
                if (managementObject["Name"] is not null)
                {
                    yield return managementObject["Name"].ToString();
                }
            }
        }

        private static IEnumerable<string> GetGPUs()
        {
            var query = from ManagementObject mo in new ManagementObjectSearcher("select * from Win32_VideoController")
                    .Get()
                where mo["Name"] != null
                select mo["Name"].ToString();

            foreach (var name in query)
            {
                yield return name;
            }
        }

        public string GetMemory()
        {
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();

            foreach (ManagementObject result in results)
            {
                return
                    $"{(ulong)result["FreePhysicalMemory"] / 1024} MB / {(ulong)result["TotalVisibleMemorySize"] / 1024} MB";
            }

            return string.Empty;
        }
    }
}
