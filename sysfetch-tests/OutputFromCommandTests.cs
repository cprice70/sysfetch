using sysfetch.Helpers;
using sysfetch.Linux;
using sysfetch.Linux.Helpers;

namespace sysfetch_tests;

public class OutputFromCommandTests
{
    [Test]
    public void TestInvalidOutputFromCommand()
    {
        // Arrange
        const string command = "command";
        const string arguments = "arguments";

        // Act
        var result = CommandHelpers.OutputFromCommand(command, arguments);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<string>());
    }
    
    [Test, Platform("Win")]
    public void TestOutputFromCommand()
    {
        // Arrange
        // "dir" is a cmd.exe builtin, not an executable.
        const string command = "cmd.exe";
        const string arguments = "/c dir C:\\";

        // Act
        var result = CommandHelpers.OutputFromCommand(command, arguments);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<string>());
        Assert.That(result, Does.Contain("Directory of"));
    }

    [Test, Platform("Linux")]
    public void TestOutputFromCommandLinux()
    {
        const string command = "uname";

        var result = CommandHelpers.OutputFromCommand(command, "");
        
        Assert.That(result, Does.Contain("Linux"));
    }
}