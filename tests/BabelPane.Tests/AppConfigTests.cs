using System.Text.Json;
using System.Windows.Input;
using Xunit;

namespace BabelPane.Tests;

public class AppConfigTests
{
    [Fact]
    public void RoundTrip_PreservesAllValues()
    {
        var original = new AppConfig
        {
            OllamaEndpoint = "http://example.test:11434",
            ModelName = "test-model:latest",
            TargetLanguage = "French",
            TimeoutSeconds = 42,
            HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift,
            HotkeyKey = Key.T,
            PaneLeft = 10.5,
            PaneTop = 20.25,
            PaneWidth = 300,
            PaneHeight = 150,
        };

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.OllamaEndpoint, roundTripped!.OllamaEndpoint);
        Assert.Equal(original.ModelName, roundTripped.ModelName);
        Assert.Equal(original.TargetLanguage, roundTripped.TargetLanguage);
        Assert.Equal(original.TimeoutSeconds, roundTripped.TimeoutSeconds);
        Assert.Equal(original.HotkeyModifiers, roundTripped.HotkeyModifiers);
        Assert.Equal(original.HotkeyKey, roundTripped.HotkeyKey);
        Assert.Equal(original.PaneLeft, roundTripped.PaneLeft);
        Assert.Equal(original.PaneTop, roundTripped.PaneTop);
        Assert.Equal(original.PaneWidth, roundTripped.PaneWidth);
        Assert.Equal(original.PaneHeight, roundTripped.PaneHeight);
    }

    [Fact]
    public void RoundTrip_PreservesNullGeometry()
    {
        var original = new AppConfig();

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(roundTripped);
        Assert.Null(roundTripped!.PaneLeft);
        Assert.Null(roundTripped.PaneTop);
        Assert.Null(roundTripped.PaneWidth);
        Assert.Null(roundTripped.PaneHeight);
    }
}
