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

    [Test]
    public void SelectGpus_DropsGenericAdaptersWhenRealGpuPresent()
    {
        var gpus = WindowsHelpers.SelectGpus(
            ["Microsoft Basic Display Adapter", "NVIDIA GeForce RTX 4080", "Microsoft Remote Display Adapter"]);

        Assert.That(gpus, Is.EqualTo(new[] { "NVIDIA GeForce RTX 4080" }));
    }

    [Test]
    public void SelectGpus_FallsBackToGenericAdapterWhenNothingElse()
    {
        // A VM without a vendor display driver (e.g. QEMU) only has the basic adapter.
        var gpus = WindowsHelpers.SelectGpus(["Microsoft Basic Display Adapter", null, ""]);

        Assert.That(gpus, Is.EqualTo(new[] { "Microsoft Basic Display Adapter" }));
    }

    [Test]
    public void SelectGpus_RemovesDuplicatesAndBlanks()
    {
        var gpus = WindowsHelpers.SelectGpus(
            ["Intel(R) UHD Graphics 770", "intel(r) uhd graphics 770", " ", null, "AMD Radeon RX 7900 XTX"]);

        Assert.That(gpus, Is.EqualTo(new[] { "Intel(R) UHD Graphics 770", "AMD Radeon RX 7900 XTX" }));
    }

    [Test]
    public void SelectGpus_EmptyWhenNoAdapters()
    {
        Assert.That(WindowsHelpers.SelectGpus([]), Is.Empty);
    }
}
