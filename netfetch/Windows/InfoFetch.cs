using Microsoft.Win32;
using System.Management;
using System.Reflection;
using System.Runtime.Intrinsics.X86;

namespace netfetch.Windows
{
    public class InfoFetch : IInfoFetch
    {
        //private readonly Dictionary<string, string> _info = new();
        private string _host;
        private string _memory;
        private string _osName;
        private string _kernel;
        private string _lastBootTime;

        private const string FMT = "O";

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
                    _host = $"{mo["Manufacturer"]} {mo["Model"]}";
                    break;
                }
            }
            #endregion

            #region OperatingSystem
            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

            foreach (ManagementObject managementObject in mos.Get())
            {
                if (managementObject["Caption"] != null)
                    _osName = managementObject["Caption"].ToString();

                if (managementObject["Version"] != null)
                    _kernel = managementObject["Version"].ToString();

                if (managementObject["LastBootUpTime"] != null)
                {
                    var boot = managementObject["LastBootUpTime"];
                    _lastBootTime = boot.ToString();
                }
            }
            #endregion
        }

        public string OSName
        {
            get
            {
                return _osName ?? string.Empty;
            }
        }

        public string Host
        {
            get
            {
                return _host ?? string.Empty;
            }
        }

        public string Kernel
        {
            get
            {
                return _kernel ?? string.Empty;
            }
        }

        public string Uptime
        {
            get
            {
                DateTime bootTime = ManagementDateTimeConverter.ToDateTime(_lastBootTime);

                return (DateTime.Now - bootTime).ToString() ?? string.Empty;
            }
        }

        public IEnumerable<string> Resolution
        {
            get
            {
                ManagementClass mc2 = new ManagementClass("Win32_DesktopMonitor");
            
                ManagementObjectCollection moc2 = mc2.GetInstances();
                if (moc2.Count != 0)
                {
                    foreach (ManagementObject mo in mc2.GetInstances())
                    {
                        // display general system information
                       if (mo["ScreenWidth"] != null && mo["ScreenHeight"] != null) 
                          yield return $"{mo["ScreenWidth"]}x{mo["ScreenHeight"]}";
                    }
                }

                yield return string.Empty;
            }
        }

        public IEnumerable<string> CPU
        {
            get
            {
                ManagementObjectSearcher mos_gpu = new ManagementObjectSearcher("select * from Win32_Processor");

                foreach (ManagementObject managementObject in mos_gpu.Get())
                {
                    if (managementObject["Name"] != null)
                    {
                        yield return managementObject["Name"].ToString();
                    }
                }
                yield return string.Empty;
            }
        }

        public IEnumerable<string> GPU
        {
            get
            {
                 ManagementObjectSearcher mos_gpu = new ManagementObjectSearcher("select * from Win32_VideoController");

                foreach (ManagementObject managementObject in mos_gpu.Get())
                {
                    if (managementObject["Name"] != null)
                    {
                        yield return managementObject["Name"].ToString();
                    }
                }
                yield return string.Empty;
            }
        }

        public string Memory 
        { 
            get
            {
                ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
                ManagementObjectCollection results = searcher.Get();

                foreach (ManagementObject result in results)
                {
                    return $"{(UInt64)result["FreePhysicalMemory"]/1024 } MB / { (UInt64)result["TotalVisibleMemorySize"]/1024 } MB";
                }
                
                return string.Empty;
            }
        }
    }
}
