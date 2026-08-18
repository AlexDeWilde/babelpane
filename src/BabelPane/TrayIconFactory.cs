using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BabelPane;

/// <summary>
/// Builds the tray icon: a bright yellow chili pepper, curved like a hook,
/// with a green tip at the top. Drawn procedurally (no design tool
/// available) as overlapping circles of shrinking radius along a curved
/// spine, rather than as a static asset.
/// </summary>
public static class TrayIconFactory
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon CreateChiliIcon()
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            DrawBody(g);
            DrawStemTip(g);
        }

        var hIcon = bitmap.GetHicon();
        try
        {
            using var iconFromHandle = Icon.FromHandle(hIcon);
            return (Icon)iconFromHandle.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    /// <summary>
    /// The pepper's body: a curved hook from the stem end (top) down to a
    /// point (bottom), drawn as overlapping circles of shrinking radius
    /// along a cubic Bezier spine — a dark-orange outline layer first, then
    /// bright yellow on top, so a thin rim stays visible at the edges.
    /// </summary>
    private static void DrawBody(Graphics g)
    {
        var p0 = new PointF(13f, 7f);
        var p1 = new PointF(29f, 10f);
        var p2 = new PointF(27f, 24f);
        var p3 = new PointF(18f, 29f);

        const int steps = 28;
        using var outlineBrush = new SolidBrush(Color.FromArgb(255, 180, 90, 0));
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var center = CubicBezier(p0, p1, p2, p3, t);
            var radius = 5.6f - 4.3f * t;
            FillCircle(g, outlineBrush, center, radius + 1.1f);
        }

        using var bodyBrush = new SolidBrush(Color.FromArgb(255, 255, 200, 0));
        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var center = CubicBezier(p0, p1, p2, p3, t);
            var radius = 5.6f - 4.3f * t;
            FillCircle(g, bodyBrush, center, radius);
        }
    }

    /// <summary>Small green calyx/stem at the very top, pointing up.</summary>
    private static void DrawStemTip(Graphics g)
    {
        using var stemBrush = new SolidBrush(Color.FromArgb(255, 46, 125, 50));
        using var path = new GraphicsPath();
        path.AddPolygon(
        [
            new PointF(10f, 8f),
            new PointF(16f, 8f),
            new PointF(13f, 1f),
        ]);
        g.FillPath(stemBrush, path);
    }

    private static void FillCircle(Graphics g, Brush brush, PointF center, float radius) =>
        g.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2, radius * 2);

    private static PointF CubicBezier(PointF p0, PointF p1, PointF p2, PointF p3, float t)
    {
        var u = 1 - t;
        var x = u * u * u * p0.X + 3 * u * u * t * p1.X + 3 * u * t * t * p2.X + t * t * t * p3.X;
        var y = u * u * u * p0.Y + 3 * u * u * t * p1.Y + 3 * u * t * t * p2.Y + t * t * t * p3.Y;
        return new PointF(x, y);
    }
}
