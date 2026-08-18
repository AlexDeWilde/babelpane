using System.Windows;

namespace BabelPane;

/// <summary>
/// Pure geometry logic for keeping the pane reachable across monitor
/// configuration changes (e.g. an external monitor unplugged since the
/// position was last saved).
/// </summary>
public static class ScreenGeometry
{
    /// <summary>
    /// Returns <paramref name="saved"/> unchanged if it overlaps at least one
    /// currently connected monitor (this also leaves alone a pane
    /// intentionally straddling two monitors). Otherwise returns a
    /// same-size rectangle centered on <paramref name="primaryWorkingArea"/>.
    /// </summary>
    public static Rect EnsureVisible(Rect saved, IReadOnlyList<Rect> monitorBounds, Rect primaryWorkingArea)
    {
        foreach (var bounds in monitorBounds)
        {
            if (bounds.IntersectsWith(saved))
            {
                return saved;
            }
        }

        var width = Math.Min(saved.Width, primaryWorkingArea.Width);
        var height = Math.Min(saved.Height, primaryWorkingArea.Height);
        var left = primaryWorkingArea.Left + (primaryWorkingArea.Width - width) / 2;
        var top = primaryWorkingArea.Top + (primaryWorkingArea.Height - height) / 2;
        return new Rect(left, top, width, height);
    }
}
