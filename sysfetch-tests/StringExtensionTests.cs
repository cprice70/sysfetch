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

    [Test]
    public void RemoveNewLines_RemovesAllNewLineCharacters()
    {
        // Arrange
        const string input = "This is a\r\n string with\n new lines.";
        const string expectedOutput = "This is a string with new lines.";

        // Act
        var output = input.RemoveNewlines();

        // Assert
        Assert.That(output, Is.EqualTo(expectedOutput));
    }
    
    [Test]
    public void TestGetValue_WithSeparatorEqual()
    {
        const string input = "param1=value1\nparam2=value2\nparam3=value3";
        const string expectedOutput = "value2";
        var actualOutput = input.GetValue("param2", "=");
        
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void TestGetValue_WithDefaultSeparator()
    {
        const string input = "param1=value1\nparam2=value2\nparam3=value3";
        const string expectedOutput = "value2";
        var actualOutput = input.GetValue("param2");
        
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void TestGetValue_ParameterNotFound()
    {
        const string input = "param1=value1\nparam2=value2\nparam3=value3";
        const string expectedOutput = "";
        var actualOutput = input.GetValue("param4");
        
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void TestGetValue_WithCustomSeparator()
    {
        const string input = "param1:value1\nparam2:value2\nparam3:value3";
        const string expectedOutput = "value2";
        var actualOutput = input.GetValue("param2", ":");
       
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }

    [Test]
    public void TestGetValue_WithQuotedValue()
    {
        const string input = "param1=value1\nparam2=\"value2 with spaces\"\nparam3=value3";
        const string expectedOutput = "value2 with spaces";
        var actualOutput = input.GetValue("param2");
        
        Assert.That(actualOutput, Is.EqualTo(expectedOutput));
    }
}