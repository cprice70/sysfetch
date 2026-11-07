using sysfetch.Windows;

namespace sysfetch_tests;

public class WindowsTests
{
    [TestCase("Windows 10 Pro", "22H2", 19045, "Windows 10 Pro 22H2")]
    [TestCase("Windows 10 Pro", "24H2", 26100, "Windows 11 Pro 24H2")]
    [TestCase("Windows 10 Home", null, 22000, "Windows 11 Home")]
    [TestCase("Windows Server 2022 Standard", "21H2", 20348, "Windows Server 2022 Standard 21H2")]
    public void FormatOSName_HandlesWindows11ProductName(string product, string? displayVersion, int build, string expected)
    {
        Assert.That(WindowsHelpers.FormatOSName(product, displayVersion, build), Is.EqualTo(expected));
    }

    [TestCase("NVIDIA GeForce RTX 4080", true)]
    [TestCase("Microsoft Basic Display Adapter", false)]
    [TestCase("Microsoft Remote Display Adapter", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsRealGpu_FiltersVirtualAdapters(string? desc, bool expected)
    {
        Assert.That(WindowsHelpers.IsRealGpu(desc), Is.EqualTo(expected));
    }
}
