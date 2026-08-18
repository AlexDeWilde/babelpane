using System.Windows;
using Xunit;

namespace BabelPane.Tests;

public class ScreenGeometryTests
{
    private static readonly Rect PrimaryMonitor = new(0, 0, 2752, 1152);
    private static readonly Rect SecondaryMonitor = new(395, -864, 2048, 864);
    private static readonly Rect PrimaryWorkingArea = new(0, 0, 2752, 1104);

    [Fact]
    public void EnsureVisible_SavedInsideAMonitor_ReturnsUnchanged()
    {
        var saved = new Rect(100, 100, 420, 220);

        var result = ScreenGeometry.EnsureVisible(saved, [PrimaryMonitor, SecondaryMonitor], PrimaryWorkingArea);

        Assert.Equal(saved, result);
    }

    [Fact]
    public void EnsureVisible_SavedStraddlingTwoMonitors_ReturnsUnchanged()
    {
        // Positioned across the primary monitor's top edge and into the secondary monitor above it.
        var saved = new Rect(500, -100, 420, 220);

        var result = ScreenGeometry.EnsureVisible(saved, [PrimaryMonitor, SecondaryMonitor], PrimaryWorkingArea);

        Assert.Equal(saved, result);
    }

    [Fact]
    public void EnsureVisible_SavedOffEveryMonitor_FallsBackToCenteredOnPrimary()
    {
        // Coordinates from a monitor that's since been unplugged.
        var saved = new Rect(5000, 5000, 420, 220);

        var result = ScreenGeometry.EnsureVisible(saved, [PrimaryMonitor, SecondaryMonitor], PrimaryWorkingArea);

        Assert.Equal(420, result.Width);
        Assert.Equal(220, result.Height);
        Assert.Equal((2752 - 420) / 2.0, result.Left);
        Assert.Equal((1104 - 220) / 2.0, result.Top);
    }

    [Fact]
    public void EnsureVisible_NoMonitors_FallsBackToCenteredOnPrimary()
    {
        var saved = new Rect(100, 100, 420, 220);

        var result = ScreenGeometry.EnsureVisible(saved, [], PrimaryWorkingArea);

        Assert.Equal((2752 - 420) / 2.0, result.Left);
        Assert.Equal((1104 - 220) / 2.0, result.Top);
    }
}
