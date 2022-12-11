using sysfetch.Extensions;

namespace sysfetch_tests;

public class ExtensionTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void StringExtensionResultTest()
    {
        string actual_value = "value1";
        // Arrange
        const string str = "param1=value1\nparam2=value2";
        const string parameter = "param1";

        // Act
        var result = str.GetValue(parameter);

        // Assert
        Assert.That(actual_value, Is.EqualTo(result));
    }
    
    [Test]
    public void StringExtensionNoResultTest()
    {
        string actual_value = "";
        // Arrange
        const string str = "param1=\nparam2=value2";
        const string parameter = "param3";

        // Act
        var result = str.GetValue(parameter);

        // Assert
        Assert.That(actual_value, Is.EqualTo(result));
    }
}