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

    private const string DisplaysJson = """
        { "SPDisplaysDataType" : [ {
            "_name" : "Apple M5 Pro",
            "spdisplays_ndrvs" : [
              { "_name" : "Color LCD", "_spdisplays_pixels" : "3456 x 2234", "_spdisplays_resolution" : "1728 x 1117 @ 120.00Hz" },
              { "_name" : "External", "_spdisplays_resolution" : "2560 x 1440 @ 60.00Hz" }
            ],
            "sppci_cores" : "20",
            "sppci_model" : "Apple M5 Pro" } ] }
        """;

    [Test]
    public void ParseDisplays_ReadsGpuAndResolutions()
    {
        var (gpus, resolutions) = MacHelpers.ParseDisplays(DisplaysJson);

        Assert.That(gpus, Is.EqualTo(new[] { "Apple M5 Pro (20 cores)" }));
        Assert.That(resolutions, Is.EqualTo(new[] { "3456x2234", "2560x1440 @ 60.00Hz" }));
    }

    [TestCase("")]
    [TestCase("not json")]
    [TestCase("{}")]
    public void ParseDisplays_HandlesBadInput(string json)
    {
        var (gpus, resolutions) = MacHelpers.ParseDisplays(json);

        Assert.That(gpus, Is.Empty);
        Assert.That(resolutions, Is.Empty);
    }
}
