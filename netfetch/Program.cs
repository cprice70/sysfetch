using netfetch;
using netfetch.Windows;

IInfoFetch info = new InfoFetch();

Console.WriteLine($"OS: {info.OSName}");
Console.WriteLine($"Host: {info.Host}");
Console.WriteLine($"Kernel: {info.Kernel}");
Console.WriteLine($"Uptime: {info.Uptime}");
Console.WriteLine($"Resolution: {info.Resolution}");
Console.WriteLine($"CPU: {info.CPU}");
Console.WriteLine($"GPU: {info.GPU}");