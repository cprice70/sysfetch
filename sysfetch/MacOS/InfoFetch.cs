using System.Text.RegularExpressions;
using System.Xml.Linq;
using sysfetch.Extensions;
using sysfetch.Linux.Helpers;
using sysfetch.MacOS.Helpers;
using sysfetch.Helpers;
using sysfetch.Linux.Helpers;

namespace sysfetch.MacOS;

public class InfoFetch : IInfoFetch
{
    public SysInfo SysInfo { get; } = new();
    
    public InfoFetch()
    {
        SysInfo.OSName = GetDistro("off");
        SysInfo.Kernel = GetDistro("off");
        SysInfo.CPU = GetCPUs("on");
        SysInfo.Model = GetModel();
        SysInfo.Uptime = GetUptime();
        SysInfo.GPUs = GetGPUs();
        SysInfo.Memory = GetMemory();
        SysInfo.Resolution = GetResolutions();
    }

    private static IEnumerable<string> GetResolutions(string refresh_rate = "off")
    {
        string resolution = "";
        string scaleFactor = "";

        var screenResolutionResult = CommandHelpers.OutputFromCommand("screenresolution", "get");
        if (!string.IsNullOrEmpty(screenResolutionResult))
        {
            var displayMatch = Regex.Match(screenResolutionResult, @"Display.+");
            if (displayMatch.Success)
            {
                resolution = displayMatch.Value.Split().Last() + "Hz, ";
            }

            resolution = Regex.Replace(resolution, @"x\?\?@", " @ ");
        }
        else
        {
            var systemProfilerResult = CommandHelpers.OutputFromCommand("system_profiler", "SPDisplaysDataType");
            var resolutionMatch = Regex.Match(systemProfilerResult, @"Resolution:.+");
            if (resolutionMatch.Success)
            {
                var resolutionParts = resolutionMatch.Value.Split().Skip(1).ToArray();
                resolution = string.Join("x", resolutionParts[0], resolutionParts[2]) + " @ " + resolutionParts[4] +
                             "Hz, ";
            }
        }

        if (File.Exists("/Library/Preferences/com.apple.windowserver.plist"))
        {
            var plistBuddyResult = CommandHelpers.OutputFromCommand("PlistBuddy",
                "-c \"Print DisplayAnyUserSets:0:0:Resolution\" /Library/Preferences/com.apple.windowserver.plist");
            scaleFactor = plistBuddyResult.Trim();
        }

        if (resolution.Contains("@ Hz"))
        {
            resolution = resolution.Replace("@ Hz", "");
        }

        if (scaleFactor.EndsWith(".0") && scaleFactor.StartsWith("2"))
        {
            resolution = resolution.Replace(" @", "@2x @");
        }

        if (refresh_rate == "off")
        {
            resolution = Regex.Replace(resolution, @" @ [0-9][0-9]Hz", "");
            resolution = Regex.Replace(resolution, @" @ [0-9][0-9][0-9]Hz", "");
        }

        if (resolution.Contains("0Hz"))
        {
            resolution = resolution.Replace(" @ 0Hz", "");
        }

        yield return resolution;
    }

    private static string GetMemory()
    {
        var hwPagesize = Convert.ToInt32(CommandHelpers.OutputFromCommand("sysctl", "-n hw.pagesize)"));
        var rawMemsize = CommandHelpers.OutputFromCommand("sysctl", "-n hw.memsize");
        var memTotal = Convert.ToInt32(rawMemsize) / 1024 / 1024;

        var rawInternalCount = CommandHelpers.OutputFromCommand("sysctl", "-n vm.page_pageable_internal_count");
        var rawPurgableCount = CommandHelpers.OutputFromCommand("sysctl", "-n vm.page_purgeable_count");
        var pagesApp = Convert.ToInt32(rawInternalCount) - Convert.ToInt32(rawPurgableCount);

        var vmstatString = CommandHelpers.OutputFromCommand("vm_stat", "");
        var pagesWired = Convert.ToInt32(vmstatString.GetValue("wired", ":"));
        var pagesCompressed = Convert.ToInt32(vmstatString.GetValue("occupied", ":"));
        
       var memUsed = ((pagesApp - pagesWired) + pagesCompressed) * hwPagesize / 1024 / 1024;
       return memUsed.ToString();
    }

    private static IEnumerable<string?> GetGPUs()
    {
        // Execute the command "system_profiler SPDisplaysDataType" and retrieve the output
        string output = CommandHelpers.OutputFromCommand("system_profiler", "SPDisplaysDataType");

        // Use a regular expression to extract the value of the "Chipset Model" field
        Regex regex = new Regex(@"^\ *Chipset Model:\s*(.*?)\s*$");
        Match match = regex.Match(output);
        if (match.Success)
        {
            var gpu = match.Groups[1].Value;

            // Remove any trailing comma and space
            gpu = gpu.TrimEnd(',', ' ');

            yield return gpu;
        }

        yield return null;
    }

    private static string GetUptime()
    {
        var boot = CommandHelpers.OutputFromCommand("sysctl", "-n kern.boottime");
 
        var bootTimeString = boot.Split(' ')[3];
        var bootTime = int.Parse(bootTimeString);

        // Get the current date in seconds
        var currentTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

        // Calculate the difference between the current time and boot time
        return (currentTime - bootTime).ToString();
    }

    private static string GetModel()
    {
        var result = CommandHelpers.OutputFromCommand("kextstat", "");
        var model = CommandHelpers.OutputFromCommand("sysctl", "-n hw.model");
        if (result.Contains("FakeSMC") || result.Contains("VirtualSMC"))
            return $"Hackintosh (SMBIOS: {model}";
        return model;
    }

    private static IEnumerable<string?> GetCPUs(string core_type)
    {
        var cpuBrandStringOutput = CommandHelpers.OutputFromCommand("sysctl", "-n machdep.cpu.brand_string");
            
        var cpuBrandString = cpuBrandStringOutput.Trim();
        var logicalCoresOutput = CommandHelpers.OutputFromCommand("sysctl", "-n hw.logicalcpu_max");
        var cores = int.Parse(logicalCoresOutput.Trim());
        
        var physicalCoresOutput = CommandHelpers.OutputFromCommand("sysctl", "-n hw.physicalcpu_max");
        var physicalCores = int.Parse(physicalCoresOutput.Trim());
        
        switch (core_type)
        {
            case "logical":
            case "on":
                yield return $"{cpuBrandString}@{cores}";
                break;
            case "physical":
                yield return $"{cpuBrandString}@{physicalCores}";
                break;
        }
    }

    private static string? GetDistro(string distroShorthand)
    {
        // Load the plist file
        var plist = XDocument.Load("/System/Library/CoreServices/SystemVersion.plist");

        if (plist is null) return null;
        
        // Extract the key-value pairs from the plist file
        var keyValues = plist.Root?
            .Elements("dict")
            .Elements("key")
            .Zip(plist.Root.Elements("dict").Elements("string"), (k, v) => new { Key = k.Value, Value = v.Value });

        if (keyValues is null) return null;
        
        // Extract the desired values from the key-value pairs
        string? darwinName = null;
        string? osxVersion = null;
        string? osxBuild = null;
        foreach (var keyValue in keyValues)
        {
            switch (keyValue.Key)
            {
                case "ProductName":
                    darwinName = keyValue.Value;
                    break;
                case "ProductVersion":
                    osxVersion = keyValue.Value;
                    break;
                case "ProductBuildVersion":
                    osxBuild = keyValue.Value;
                    break;
            }
        }

        if (string.IsNullOrEmpty(osxVersion) || string.IsNullOrEmpty(osxBuild)) return null;
        return MacHelpers.GetDistroInfo(osxVersion, osxBuild, distroShorthand);
    }
}