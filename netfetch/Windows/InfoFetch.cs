using Microsoft.Win32;
using System.Management;
using System.Reflection;
using System.Runtime.Intrinsics.X86;

namespace netfetch.Windows
{
    public class InfoFetch : IInfoFetch
    {
        private readonly Dictionary<string, string> _info = new Dictionary<string, string>();
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
                    _info["host"] = $"{mo["Manufacturer"]} {mo["Model"]}";
                }
            }
            #endregion
            #region DesktopMonitor
            ManagementClass mc2 = new ManagementClass("Win32_DesktopMonitor");
            //collection to store all management objects

            ManagementObjectCollection moc2 = mc.GetInstances();
            if (moc2.Count != 0)
            {
                foreach (ManagementObject mo in mc2.GetInstances())
                {
                    // display general system information
                    _info["resolution"] = $"{mo["ScreenWidth"]}x{mo["ScreenHeight"]}";
                }
            }
            #endregion
            #region CPU
            ManagementClass mc3 = new ManagementClass("Win32_Processor");
            //collection to store all management objects

            ManagementObjectCollection moc3 = mc.GetInstances();
            if (moc3.Count != 0)
            {
                foreach (ManagementObject mo in mc3.GetInstances())
                {
                    // display general system information
                    _info["cpu"] = $"{mo["Name"]}";
                }
            }
            #endregion
            #region GPU
            ManagementObjectSearcher mos_gpu = new ManagementObjectSearcher("select * from Win32_VideoController");

            foreach (ManagementObject managementObject in mos_gpu.Get())
            {
                if (managementObject["Name"] != null)
                {
                    _info["gpu"] = managementObject["Name"].ToString();
                }
            }
            #endregion

            #region OperatingSystem
            ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

            foreach (ManagementObject managementObject in mos.Get())
            {
                if (managementObject["Caption"] != null)
                    _info["osname"] = managementObject["Caption"].ToString();

                if (managementObject["Version"] != null)
                    _info["version"] = managementObject["Version"].ToString();

                if (managementObject["LastBootUpTime"] != null)
                {

                    var boot = managementObject["LastBootUpTime"];
                    _info["lastboottime"] = boot.ToString();
                }
            }
            #endregion
        }

        public string OSName
        {
            get
            {
                _info.TryGetValue("osname", out string? value);
                return value ?? string.Empty;
            }
        }

        public string Host
        {
            get
            {
                _info.TryGetValue("host", out var host);
                return host ?? string.Empty;
            }
        }

        public string Kernel
        {
            get
            {
                _info.TryGetValue("version", out string? version);
                return version ?? string.Empty;
            }
        }

        public string Uptime
        {
            get
            {
                _info.TryGetValue("lastboottime", out string? lastboottime);
                DateTime bootTime = ManagementDateTimeConverter.ToDateTime(lastboottime);

                return (DateTime.Now - bootTime).ToString() ?? string.Empty;
            }
        }

        public string Resolution
        {
            get
            {
                _info.TryGetValue("resolution", out string? resolution);
                return resolution ?? string.Empty;
            }
        }

        public string CPU
        {
            get
            {
                _info.TryGetValue("cpu", out string? cpu);
                return cpu ?? string.Empty;
            }
        }

        public string GPU
        {
            get
            {
                _info.TryGetValue("gpu", out string? gpu);
                return gpu ?? string.Empty;
            }
        }
    }
}
