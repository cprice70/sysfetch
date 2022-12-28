using System.Text.RegularExpressions;
using sysfetch.Helpers;
using sysfetch.Linux.Helpers;

namespace sysfetch.MacOS;

public class InfoFetch : IInfoFetch
{
    public SysInfo SysInfo { get; } = new();
    
    public InfoFetch()
    {
        SysInfo.OSName = "Linux";
        SysInfo.Kernel = GetDistro("off");
        SysInfo.CPU = GetCPUs();
        SysInfo.Model = GetModel();
        SysInfo.Uptime = GetUptime();
        SysInfo.GPUs = GetGPUs();
        SysInfo.Memory = GetMemory();
        SysInfo.Resolution = GetResolutions();
    }

    private void CacheUname()
    {
        var result = CommandHelpers.OutputFromCommand("uname", "-srm");
        var uname = result.Split(" ");
        var kernelName = uname[0];
        var kernelVersion = uname[1];
        var kernelMachine = uname[2];

        if (kernelName == "Darwin")
        {
            SetSystemVersionCompat(0);

            string swVersOutput = GetSwVersOutput();
            string[] swVersLines = swVersOutput.Split('\n');
            string[] swVers = swVersLines.Select(line => Regex.Match(line, @"<key>|</key><string>(.*?)</string>").Groups[1].Value).ToArray();

            for (int i = 0; i < swVers.Length; i += 2)
            {
                switch (swVers[i])
                {
                    case "ProductName":
                        string darwinName = swVers[i + 1];
                        break;
                    case "ProductVersion":
                        string osxVersion = swVers[i + 1];
                        break;
                    case "ProductBuildVersion":
                        string osxBuild = swVers[i + 1];
                        break;
                }
            }
        }
    }
    
    private IEnumerable<string> GetResolutions()
    {
        throw new NotImplementedException();
    }

    private string GetMemory()
    {
        throw new NotImplementedException();
    }

    private IEnumerable<string> GetGPUs()
    {
        throw new NotImplementedException();
    }

    private string GetUptime()
    {
        throw new NotImplementedException();
    }

    private string GetModel()
    {
        throw new NotImplementedException();
    }

    private IEnumerable<string?> GetCPUs()
    {
        throw new NotImplementedException();
    }

    private string? GetDistro(string off)
    {
        throw new NotImplementedException();
    }
}