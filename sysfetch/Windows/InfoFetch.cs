using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using sysfetch.Extensions;
using sysfetch.Helpers;

namespace sysfetch.Windows
{
    [SupportedOSPlatform("windows")]
    public partial class InfoFetch : IInfoFetch
    {
        public SysInfo SysInfo { get; } = new();

        private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        private const string BiosKey = @"HARDWARE\DESCRIPTION\System\BIOS";
        private const string CpuKey = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";
        private const string DisplayClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        public InfoFetch() : this(Probe.DefaultTimeout) { }

        public InfoFetch(TimeSpan timeout)
        {
            var host = Probe.Start(GetModel);
            var os = Probe.Start(GetOSName);
            var kernel = Probe.Start(GetKernel);
            var cpu = Probe.Start(() => GetCPUs().ToList());
            var resolutions = Probe.Start(() => GetResolutions().ToList());
            var gpus = Probe.Start(() => GetGPUs().ToList());
            var memory = Probe.Start(GetMemory);

            Probe.WaitAll(timeout, host, os, kernel, cpu, resolutions, gpus, memory);

            SysInfo.Host = Probe.Result(host, string.Empty);
            SysInfo.OSName = Probe.Result(os, string.Empty);
            SysInfo.OSId = "windows";
            SysInfo.Kernel = Probe.Result(kernel, string.Empty);
            SysInfo.Uptime = ((int)(Environment.TickCount64 / 1000)).GetHumanReadableString();
            SysInfo.CPU = Probe.Result(cpu, []);
            SysInfo.Resolution = Probe.Result(resolutions, []);
            SysInfo.GPUs = Probe.Result(gpus, []);
            SysInfo.Memory = Probe.Result(memory, string.Empty);
        }

        private static string? ReadString(string key, string name) =>
            Registry.LocalMachine.OpenSubKey(key)?.GetValue(name)?.ToString()?.Trim();

        private static string GetModel()
        {
            var vendor = ReadString(BiosKey, "SystemManufacturer");
            var product = ReadString(BiosKey, "SystemProductName");
            return $"{vendor} {product}".Trim();
        }

        private static string GetOSName()
        {
            var product = ReadString(CurrentVersionKey, "ProductName") ?? "Windows";
            var displayVersion = ReadString(CurrentVersionKey, "DisplayVersion");
            int.TryParse(ReadString(CurrentVersionKey, "CurrentBuildNumber"), out var build);
            return WindowsHelpers.FormatOSName(product, displayVersion, build);
        }

        private static string GetKernel()
        {
            var version = Environment.OSVersion.Version;
            var ubr = Registry.LocalMachine.OpenSubKey(CurrentVersionKey)?.GetValue("UBR");
            return ubr is int revision
                ? $"{version.Major}.{version.Minor}.{version.Build}.{revision}"
                : version.ToString();
        }

        private static IEnumerable<string?> GetCPUs()
        {
            var name = ReadString(CpuKey, "ProcessorNameString");
            if (string.IsNullOrEmpty(name)) yield break;
            yield return $"{name} ({Environment.ProcessorCount})";
        }

        private static IEnumerable<string> GetGPUs()
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(DisplayClassKey);
            if (classKey is null) return [];

            var descriptions = new List<string?>();
            foreach (var sub in classKey.GetSubKeyNames())
            {
                if (!int.TryParse(sub, out _)) continue; // skip "Properties" etc.
                using var adapter = classKey.OpenSubKey(sub);
                descriptions.Add(adapter?.GetValue("DriverDesc")?.ToString());
            }

            return WindowsHelpers.SelectGpus(descriptions);
        }

        private static string GetMemory()
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (!GlobalMemoryStatusEx(ref status)) return string.Empty;

            var total = status.TotalPhys / 1024 / 1024;
            var used = (status.TotalPhys - status.AvailPhys) / 1024 / 1024;
            return $"{used} MiB / {total} MiB";
        }

        private static unsafe IEnumerable<string> GetResolutions()
        {
            var results = new List<string>();
            const int displayDeviceSize = 840;
            const int devModeSize = 220;
            const uint attachedToDesktop = 0x1;
            const int enumCurrentSettings = -1;

            var device = stackalloc byte[displayDeviceSize];
            var mode = stackalloc byte[devModeSize];

            for (uint i = 0; ; i++)
            {
                new Span<byte>(device, displayDeviceSize).Clear();
                *(uint*)device = displayDeviceSize; // cb
                if (!EnumDisplayDevicesW(null, i, device, 0)) break;

                var stateFlags = *(uint*)(device + 324);
                if ((stateFlags & attachedToDesktop) == 0) continue;

                new Span<byte>(mode, devModeSize).Clear();
                *(ushort*)(mode + 68) = devModeSize; // dmSize
                var deviceName = new string((char*)(device + 4));
                if (!EnumDisplaySettingsW(deviceName, enumCurrentSettings, mode)) continue;

                var width = *(uint*)(mode + 172);
                var height = *(uint*)(mode + 176);
                var hz = *(uint*)(mode + 184);
                results.Add(hz > 1 ? $"{width}x{height} @ {hz}Hz" : $"{width}x{height}");
            }

            return results;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool EnumDisplayDevicesW(string? device, uint devNum, byte* displayDevice, uint flags);

        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool EnumDisplaySettingsW(string deviceName, int modeNum, byte* devMode);
    }

    public static class WindowsHelpers
    {
        /// <summary>
        /// Windows 11 still reports "Windows 10" in ProductName; the build number (22000+) tells them apart.
        /// </summary>
        public static string FormatOSName(string productName, string? displayVersion, int build)
        {
            var name = build >= 22000 ? productName.Replace("Windows 10", "Windows 11") : productName;
            return string.IsNullOrEmpty(displayVersion) ? name : $"{name} {displayVersion}";
        }

        /// <summary>
        /// Distinct adapter names, without generic Microsoft adapters when a real GPU is present.
        /// A machine with no vendor driver (common in VMs) only has the generic adapter, so it is
        /// shown rather than leaving the GPU line empty.
        /// </summary>
        public static List<string> SelectGpus(IEnumerable<string?> driverDescs)
        {
            var all = driverDescs
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var real = all.Where(IsRealGpu).ToList();
            return real.Count > 0 ? real : all;
        }

        public static bool IsRealGpu(string? driverDesc) =>
            !string.IsNullOrWhiteSpace(driverDesc)
            && !driverDesc.Contains("Basic Display", StringComparison.OrdinalIgnoreCase)
            && !driverDesc.Contains("Basic Render", StringComparison.OrdinalIgnoreCase)
            && !driverDesc.Contains("Remote Display", StringComparison.OrdinalIgnoreCase)
            && !driverDesc.Contains("Indirect Display", StringComparison.OrdinalIgnoreCase);
    }
}
