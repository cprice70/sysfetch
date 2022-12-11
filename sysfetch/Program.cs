using sysfetch;
using System.Runtime.InteropServices;

IInfoFetch? info = null;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    info = new sysfetch.Windows.InfoFetch();
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    info = new sysfetch.Linux.InfoFetch();

if (info is null)
{
    Console.WriteLine("Platform is not suppported");
    Environment.Exit(0);
}

Console.WriteLine($"OS: {info.SysInfo.OSName}");
Console.WriteLine($"Host: {info.SysInfo.Host}");
Console.WriteLine($"Kernel: {info.SysInfo.Kernel}");
Console.WriteLine($"Uptime: {info.SysInfo.Uptime}");

if (info.SysInfo.Resolution is not null)
{
    foreach (var resolution in info.SysInfo.Resolution)
    { 
        if (!string.IsNullOrEmpty(resolution))
            Console.WriteLine($"Resolution: {resolution}");
    }
}
if (info.SysInfo.CPU is not null)
{
    foreach ( var cpu in info.SysInfo.CPU)
    {
        if (!string.IsNullOrEmpty(cpu))
            Console.WriteLine($"CPU: {cpu}");
    }
}

if (info.SysInfo.GPUs is not null)
{
    foreach ( var gpu in info.SysInfo.GPUs)
    {
        if ( !string.IsNullOrEmpty(gpu))
            Console.WriteLine($"GPU: {gpu}");
    }
}

Console.WriteLine($"Memory: {info.SysInfo.Memory}");