using System.Collections;
using sysfetch.Helpers;
using sysfetch.Linux;
using sysfetch.Linux.Helpers;

namespace sysfetch_tests;

public class LinuxHelperTests
{
    private const string distroString = "DISTRIB_ID=Ubuntu\nDISTRIB_RELEASE=22.10\nDISTRIB_CODENAME=kinetic\nDISTRIB_DESCRIPTION=\"Ubuntu 22.10\"";

    private const string distroString2 =
        "PRETTY_NAME=\"Ubuntu 22.10\"\nNAME=\"Ubuntu\"\nVERSION_ID=\"22.10\"\nVERSION=\"22.10 (Kinetic Kudu)\"\nVERSION_CODENAME=kinetic" +
        "ID=ubuntu\nID_LIKE=debian\nHOME_URL=\"https://www.ubuntu.com/\"\nSUPPORT_URL=\"https://help.ubuntu.com/\"" +
        "BUG_REPORT_URL=\"https://bugs.launchpad.net/ubuntu/\"\nPRIVACY_POLICY_URL=\"https://www.ubuntu.com/legal/terms-and-policies/privacy-policy\"" +
        "UBUNTU_CODENAME=kinetic\nLOGO=ubuntu-logo";

    private const string IntelDevice =
        "00:02.0 \"VGA compatible controller\" [0300]: \"Intel Corporation\" 82945G/GZ \"Integrated Graphics Controller\" [8086:2772] (rev 02)";

    private const string NvidiaDevice =
        "01:00.0 \"VGA compatible controller\" [0300]: \"NVIDIA Corporation GP104 [GeForce GTX 1080]\" [10de:1b80] (rev a1)";
    private const string Nvidia2080Device =
        "01:00.0 VGA compatible controller [0300]: NVIDIA Corporation GP104 [GeForce GTX 2080] [10de:1b80] (rev a1)";

    private const string AMDDevice =
        "01:00.0 VGA compatible controller [0300]: Advanced Micro Devices, Inc. [AMD/ATI] Navi 10 [Radeon RX 5600 XT] [1002:731f]";

    private const string IntelResult =
        "Intel Integrated Graphics Controller";

    private const string AMDResult =
        "AMD ATI Radeon RX 5600 XT";

    private const string NvidiaResult =
        "NVIDIA GeForce GTX 1080";
    
    public LinuxHelperTests()
    {

    }
    
   [Test]
   [TestCase(distroString, "Ubuntu 22.10")]
   [TestCase(distroString2, "Ubuntu 22.10")]
    public void TestParseDistroStrings_Off(string inputStr, string correctResult)
    {
        // Arrange
        string[] fileInfo = { inputStr };
        const string distroShorthand = "off";

        // Act
        var result = LinuxHelpers.ParseDistroStrings(fileInfo, distroShorthand);

        // Assert
        Assert.That(result, Is.EqualTo(correctResult));
    }

    [Test]
    [TestCase(distroString, "Ubuntu 22.10")]
    [TestCase(distroString2, "Ubuntu 22.10")]
    public void TestParseDistroStrings_On(string inputStr, string correctResult)
    {
        // Arrange
        string[] fileInfo = { inputStr };
        const string distroShorthand = "on";

        // Act
        var result = LinuxHelpers.ParseDistroStrings(fileInfo, distroShorthand);

        // Assert
        Assert.That(result, Is.EqualTo(correctResult));
    }

    [Test]
    [TestCase(distroString)]
    [TestCase(distroString2)]
    public void TestParseDistroStrings_Tiny(string inputStr)
    {
        // Arrange
        string[] fileInfo = { inputStr };
        const string distroShorthand = "tiny";

        // Act
        var result = LinuxHelpers.ParseDistroStrings(fileInfo, distroShorthand);

        // Assert
        Assert.That(result, Is.EqualTo("Ubuntu"));
    }
    
    [Test]
    public void TestParseDistroStrings_EmptyFileInfo()
    {
        // Arrange
        string[] fileInfo = Array.Empty<string>();
        const string distroShorthand = "on";

        // Act
        var result = LinuxHelpers.ParseDistroStrings(fileInfo, distroShorthand);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }
    
    [Test]
    public void TestEmptyGPUArray()
    {
        string[] gpus = { };
        const string gpuType = "dedicated";
        var result = LinuxHelpers.ParseGPUStrings(gpus, gpuType);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void TestMixedGPUArray()
    {
        string[] gpus = { IntelDevice, IntelDevice, AMDDevice, NvidiaDevice};
        const string gpuType = "dedicated";
        var result = LinuxHelpers.ParseGPUStrings(gpus, gpuType);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result, Does.Contain(AMDResult));
        Assert.That(result, Does.Contain(NvidiaResult));
    }

    [Test]
    public void TestIntegratedGPUType()
    {
        string[] gpus = { IntelDevice, AMDDevice, NvidiaDevice};
        const string gpuType = "integrated";
        var result = LinuxHelpers.ParseGPUStrings(gpus, gpuType);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result, Does.Contain(IntelResult));
    }

    [Test]
    public void TestOffGPUType()
    {
        string[] gpus = { IntelDevice, AMDDevice, NvidiaDevice };
        const string gpuType = "off";
        var result = LinuxHelpers.ParseGPUStrings(gpus, gpuType);
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result, Does.Contain(IntelResult));
        Assert.That(result, Does.Contain(AMDResult));
        Assert.That(result, Does.Contain(NvidiaResult));
    }

    [Test]
    public void TestAdvancedGPU()
    {
        string[] gpus = { AMDDevice };
        string gpuType = "dedicated";
        var result = LinuxHelpers.ParseGPUStrings(gpus, gpuType);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result, Does.Contain(AMDResult));
    }

    [Test]
    public void TestIntelAndNVIDIAGPU()
    {
        string[] gpus = { IntelDevice, NvidiaDevice };
        const string gpuType = "dedicated";
        var result = LinuxHelpers.ParseGPUStrings(gpus, gpuType);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result, Does.Contain(NvidiaResult));
    }

    [Test]
    [TestCase(IntelDevice, "\"Intel Corporation\" \"Integrated Graphics Controller\"")]
    [TestCase(NvidiaDevice, "\"NVIDIA Corporation GP104 [GeForce GTX 1080]\"")]
    public void TestGPUParseData(string deviceString, string correctValue)
    {
        var result = LinuxHelpers.ParseDeviceString(deviceString);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result, Does.Contain(correctValue));
    }

    [Test, Platform("Linux")]
    public void TestIsCommandAvailable_True()
    {
        var result = CommandHelpers.IsCommandAvailable("ls");
        
        Assert.That(result, Is.True);
    }
    
    [Test, Platform("Linux")]
    public void TestIsCommandAvailable_False()
    {
        var result = CommandHelpers.IsCommandAvailable("__");
        
        Assert.That(result, Is.False);
    }
}