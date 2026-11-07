using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace sysfetch.MacOS;

[SupportedOSPlatform("macos")]
internal static unsafe partial class Native
{
    private const string LibSystem = "/usr/lib/libSystem.dylib";

    [LibraryImport(LibSystem, EntryPoint = "sysctlbyname", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SysctlByName(string name, void* oldp, nuint* oldlenp, void* newp, nuint newlen);

    [LibraryImport(LibSystem, EntryPoint = "mach_host_self")]
    private static partial uint MachHostSelf();

    [LibraryImport(LibSystem, EntryPoint = "host_statistics64")]
    private static partial int HostStatistics64(uint host, int flavor, VmStatistics64* info, uint* count);

    private const int HostVmInfo64 = 4;

    public static string? GetString(string name)
    {
        nuint size = 0;
        if (SysctlByName(name, null, &size, null, 0) != 0 || size == 0) return null;

        var buffer = new byte[(int)size];
        fixed (byte* p = buffer)
        {
            if (SysctlByName(name, p, &size, null, 0) != 0) return null;
        }

        var length = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, length < 0 ? (int)size : length).Trim();
    }

    public static long? GetInt64(string name)
    {
        long value = 0;
        nuint size = sizeof(long);
        if (SysctlByName(name, &value, &size, null, 0) != 0) return null;

        // Some sysctls are 32-bit; the high bytes stay zero on little-endian.
        return size == sizeof(int) ? (int)value : value;
    }

    public static TimeVal? GetTimeVal(string name)
    {
        TimeVal value;
        nuint size = (nuint)sizeof(TimeVal);
        if (SysctlByName(name, &value, &size, null, 0) != 0) return null;
        return value;
    }

    public static VmStatistics64? GetVmStatistics()
    {
        VmStatistics64 stats;
        uint count = (uint)(sizeof(VmStatistics64) / sizeof(int));
        if (HostStatistics64(MachHostSelf(), HostVmInfo64, &stats, &count) != 0) return null;
        return stats;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TimeVal
    {
        public long Seconds;
        public int Microseconds;
    }

    // Mirrors struct vm_statistics64 from <mach/vm_statistics.h>.
    [StructLayout(LayoutKind.Sequential)]
    public struct VmStatistics64
    {
        public uint FreeCount;
        public uint ActiveCount;
        public uint InactiveCount;
        public uint WireCount;
        public ulong ZeroFillCount;
        public ulong Reactivations;
        public ulong Pageins;
        public ulong Pageouts;
        public ulong Faults;
        public ulong CowFaults;
        public ulong Lookups;
        public ulong Hits;
        public ulong Purges;
        public uint PurgeableCount;
        public uint SpeculativeCount;
        public ulong Decompressions;
        public ulong Compressions;
        public ulong Swapins;
        public ulong Swapouts;
        public uint CompressorPageCount;
        public uint ThrottledCount;
        public uint ExternalPageCount;
        public uint InternalPageCount;
        public ulong TotalUncompressedPagesInCompressor;
    }
}
