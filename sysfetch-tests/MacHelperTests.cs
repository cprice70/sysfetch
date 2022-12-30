using sysfetch.MacOS.Helpers;

namespace sysfetch_tests;

[TestFixture]
public class MacHelperTests
{
    [TestCase("10.15", "macOS Catalina", "22A400", "off", "macOS 10.15 22A400", TestName = "Catalina test")]
    [TestCase("10.16", "macOS Big Sur", "22A400", "on", "macOS 10.16", TestName = "Big Sur test")]
    [TestCase("11.0", "macOS Big Sur", "22A400", "tiny", "macOS 11.0", TestName = "Big Sur 11.0 test")]
    public void MacVersionTests(string osxVersion, string expectedCodename, string osxBuild, string distroShorthand, string expectedDistro)
    {
        var distro = MacHelpers.GetDistroInfo(osxVersion, osxBuild, distroShorthand);
        
        Assert.That(distro, Is.EqualTo(expectedDistro));
    }
}