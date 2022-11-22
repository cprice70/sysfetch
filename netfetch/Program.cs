using netfetch;
using netfetch.Windows;

IInfoFetch info = new InfoFetch();

Console.WriteLine($"OS: {info.OSName}");
Console.WriteLine($"Host: {info.Host}");
Console.WriteLine($"Kernel: {info.Kernel}");
Console.WriteLine($"Uptime: {info.Uptime}");

foreach (var resolution in info.Resolution)
{ 
    if (!string.IsNullOrEmpty(resolution))
        Console.WriteLine($"Resolution: {resolution}");
}

foreach ( var cpu in info.CPU)
{
    if (!string.IsNullOrEmpty(cpu))
        Console.WriteLine($"CPU: {cpu}");
}

foreach ( var gpu in info.GPU)
{
    if ( !string.IsNullOrEmpty(gpu))
        Console.WriteLine($"GPU: {gpu}");
}

Console.WriteLine($"Memory: {info.Memory}");