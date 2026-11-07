using sysfetch.Linux.Helpers;

namespace sysfetch_tests;

public class LinuxHardwareTests
{
    private const string PciIds = """
        # comment line
        8086  Intel Corporation
        	3e9b  CoffeeLake-H GT2 [UHD Graphics 630]
        	46a6  Alder Lake-P GT2 [Iris Xe Graphics]
        		1028 0b19  Some subsystem
        10de  NVIDIA Corporation
        	1b80  GP104 [GeForce GTX 1080]
        	2684  AD102 [GeForce RTX 4090]
        1af4  Red Hat, Inc.
        c0a9  Micron/Crucial Technology
        C 00  Unclassified device
        	00  Non-VGA unclassified device
        """;

    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("sysfetch-tests-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_root, recursive: true);
    }

    private void AddPciDevice(string slot, string cls, string vendor, string device)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_root, "pci", slot)).FullName;
        File.WriteAllText(Path.Combine(dir, "class"), cls + "\n");
        File.WriteAllText(Path.Combine(dir, "vendor"), vendor + "\n");
        File.WriteAllText(Path.Combine(dir, "device"), device + "\n");
    }

    private string WritePciIds()
    {
        var path = Path.Combine(_root, "pci.ids");
        File.WriteAllText(path, PciIds.Replace("\r\n", "\n"));
        return path;
    }

    [Test]
    public void GetGpusFromSysfs_ReadsDisplayClassDevicesOnly()
    {
        AddPciDevice("0000:00:02.0", "0x030000", "0x8086", "0x3e9b");
        AddPciDevice("0000:01:00.0", "0x030000", "0x10de", "0x2684");
        AddPciDevice("0000:00:1f.3", "0x040300", "0x8086", "0xa348"); // audio, ignored

        var gpus = LinuxHardware.GetGpusFromSysfs(Path.Combine(_root, "pci"), [WritePciIds()]);

        Assert.That(gpus, Is.EqualTo(new[] { "Intel UHD Graphics 630", "NVIDIA GeForce RTX 4090" }));
    }

    [Test]
    public void GetGpusFromSysfs_UnknownDeviceWithoutPciIds()
    {
        AddPciDevice("0000:00:02.0", "0x030000", "0x1af4", "0x1050");

        var gpus = LinuxHardware.GetGpusFromSysfs(Path.Combine(_root, "pci"), [Path.Combine(_root, "missing.ids")]);

        Assert.That(gpus, Is.EqualTo(new[] { "Virtio Device 1050" }));
    }

    [Test]
    public void GetGpusFromSysfs_MissingRoot()
    {
        Assert.That(LinuxHardware.GetGpusFromSysfs(Path.Combine(_root, "nope")), Is.Empty);
    }

    [Test]
    public void LookupPciIds_SkipsSubsystemsAndStopsAtClasses()
    {
        var names = LinuxHardware.LookupPciIds(WritePciIds(), [("8086", "46a6"), ("c0a9", "0000")]);

        Assert.That(names["8086"], Is.EqualTo("Intel Corporation"));
        Assert.That(names["8086:46a6"], Is.EqualTo("Alder Lake-P GT2 [Iris Xe Graphics]"));
        Assert.That(names["c0a9"], Is.EqualTo("Micron/Crucial Technology"));
        Assert.That(names.ContainsKey("c0a9:0000"), Is.False);
    }

    [TestCase("1002", "Advanced Micro Devices, Inc. [AMD/ATI]", "731f", "Navi 10 [Radeon RX 5600 OEM/5600 XT / 5700/5700 XT]", "AMD Radeon RX 5600 OEM/5600 XT / 5700/5700 XT")]
    [TestCase("8086", "Intel Corporation", "5917", "UHD Graphics 620", "Intel UHD Graphics 620")]
    [TestCase("abcd", "Acme Corporation", "0001", "Widget GPU", "Acme Widget GPU")]
    [TestCase("abcd", null, "0001", null, "Vendor abcd Device 0001")]
    public void FormatGpu(string vendorId, string? vendorName, string deviceId, string? deviceName, string expected)
    {
        Assert.That(LinuxHardware.FormatGpu(vendorId, vendorName, deviceId, deviceName), Is.EqualTo(expected));
    }

    [Test]
    public void GetResolutionsFromDrm_OnlyConnectedConnectors()
    {
        void Connector(string name, string status, string modes)
        {
            var dir = Directory.CreateDirectory(Path.Combine(_root, "drm", name)).FullName;
            File.WriteAllText(Path.Combine(dir, "status"), status + "\n");
            File.WriteAllText(Path.Combine(dir, "modes"), modes);
        }

        Connector("card0-eDP-1", "connected", "2560x1600\n1920x1200\n");
        Connector("card0-HDMI-A-1", "disconnected", "");
        Connector("card1-DP-2", "connected", "3840x2160\n");
        Directory.CreateDirectory(Path.Combine(_root, "drm", "card0")); // device node, not a connector

        var modes = LinuxHardware.GetResolutionsFromDrm(Path.Combine(_root, "drm"));

        Assert.That(modes, Is.EqualTo(new[] { "2560x1600", "3840x2160" }));
    }

    [TestCase("processor\t: 0\nmodel name\t: AMD Ryzen 9 7950X 16-Core Processor\n", "AMD Ryzen 9 7950X 16-Core Processor")]
    [TestCase("processor\t: 0\nBogoMIPS\t: 108.00\nHardware\t: BCM2835\n", "BCM2835")]
    [TestCase("processor\t: 0\nBogoMIPS\t: 108.00\n", null)]
    public void ParseCpuName(string cpuInfo, string? expected)
    {
        Assert.That(LinuxHardware.ParseCpuName(cpuInfo), Is.EqualTo(expected));
    }
}
