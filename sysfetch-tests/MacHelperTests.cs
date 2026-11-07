using sysfetch.MacOS.Helpers;

namespace sysfetch_tests;

[TestFixture]
public class MacHelperTests
{
    [TestCase("10.15", "macOS Catalina", "22A400", "off", "macOS 10.15 22A400", TestName = "Catalina off")]
    [TestCase("10.16", "macOS Big Sur", "22A400", "on", "macOS 10.16", TestName = "Big Sur on")]
    [TestCase("11.0", "macOS Big Sur", "22A400", "tiny", "macOS 11.0", TestName = "Big Sur 11.0 tiny")]
    [TestCase("11.0", "macOS Big Sur", "22A400", "invalid", "macOS 11.0 22A400", TestName = "Big Sur 11.0 invalid")]
    public void MacVersionTests(string osxVersion, string expectedCodename, string osxBuild, string distroShorthand, string expectedDistro)
    {
        var distro = MacHelpers.GetDistroInfo(osxVersion, osxBuild, distroShorthand);
        
        Assert.That(distro, Is.EqualTo(expectedDistro));
    }
}
