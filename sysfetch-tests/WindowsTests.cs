using System.Management;

namespace sysfetch_tests;

[TestFixture, Platform("Win")]
public class WindowsTests
{
    [Test]
    public void GetUptime_ReturnsCorrectUptime()
    {
        // Arrange
        var lastBootTime = ManagementDateTimeConverter.ToDateTime("2022-12-28T12:00:00");

        // Act
        var uptime = sysfetch.Windows.InfoFetch.GetUptime(lastBootTime.ToString());

        // Assert
        Assert.AreEqual("12:00:00", uptime);
    }

    [Test]
    public void GetUptime_HandlesNullInput()
    {
        // Arrange
        string lastBootTime = null;

        // Act
        var uptime = sysfetch.Windows.InfoFetch.GetUptime(lastBootTime);

        // Assert
        Assert.AreEqual("0:00:00", uptime);
    }
}