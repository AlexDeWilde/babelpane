using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace BabelPane;

/// <summary>
/// Captures the screen pixels beneath a window's bounds, in memory only.
/// </summary>
public static class ScreenCapture
{
    /// <summary>
    /// Hides the window, captures whatever is now visible in its former
    /// screen bounds, restores the window, and returns the capture as PNG
    /// bytes. Hiding first guarantees the pane never captures its own chrome.
    /// </summary>
    public static byte[] CapturePaneRegion(Window window)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        var bounds = new Rectangle(
            (int)Math.Round(window.Left * dpi.DpiScaleX),
            (int)Math.Round(window.Top * dpi.DpiScaleY),
            (int)Math.Round(window.ActualWidth * dpi.DpiScaleX),
            (int)Math.Round(window.ActualHeight * dpi.DpiScaleY));

        var wasVisible = window.Visibility == Visibility.Visible;
        if (wasVisible)
        {
            window.Hide();
            // Give the compositor a moment to redraw the now-uncovered region.
            Thread.Sleep(120);
        }

        try
        {
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        finally
        {
            if (wasVisible)
            {
                window.Show();
                window.Activate();
            }
        }
    }
}
