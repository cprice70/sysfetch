using sysfetch.Linux;

namespace sysfetch_tests;

public class LinuxHelperTests
{
    private const string distroString = "DISTRIB_ID=Ubuntu\nDISTRIB_RELEASE=22.10\nDISTRIB_CODENAME=kinetic\nDISTRIB_DESCRIPTION=\"Ubuntu 22.10\"";

    private const string distroString2 =
        "PRETTY_NAME=\"Ubuntu 22.10\"\nNAME=\"Ubuntu\"\nVERSION_ID=\"22.10\"\nVERSION=\"22.10 (Kinetic Kudu)\"\nVERSION_CODENAME=kinetic" +
        "ID=ubuntu\nID_LIKE=debian\nHOME_URL=\"https://www.ubuntu.com/\"\nSUPPORT_URL=\"https://help.ubuntu.com/\"" +
        "BUG_REPORT_URL=\"https://bugs.launchpad.net/ubuntu/\"\nPRIVACY_POLICY_URL=\"https://www.ubuntu.com/legal/terms-and-policies/privacy-policy\"" +
        "UBUNTU_CODENAME=kinetic\nLOGO=ubuntu-logo";
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
}