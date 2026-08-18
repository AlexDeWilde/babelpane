using Xunit;

namespace BabelPane.Tests;

public class OllamaClientTests
{
    [Fact]
    public void ExtractResponseText_ReturnsResponseField()
    {
        var body = """{"model":"test","response":"Bonjour le monde","done":true}""";

        var result = OllamaClient.ExtractResponseText(body);

        Assert.Equal("Bonjour le monde", result);
    }

    [Fact]
    public void ExtractResponseText_ReturnsEmpty_WhenResponseIsNull()
    {
        var body = """{"model":"test","response":null,"done":true}""";

        var result = OllamaClient.ExtractResponseText(body);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractResponseText_PreservesParagraphBreaks()
    {
        var body = """{"response":"Line one\n\nLine two"}""";

        var result = OllamaClient.ExtractResponseText(body);

        Assert.Equal("Line one\n\nLine two", result);
    }
}
