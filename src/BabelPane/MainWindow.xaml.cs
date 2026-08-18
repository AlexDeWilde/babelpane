using System.Windows;
using System.Windows.Input;

namespace BabelPane;

public enum PaneState
{
    Closed,
    Open,
    Busy,
    Triggered,
}

/// <summary>
/// The floating overlay pane. Drives the hotkey's open -> trigger -> close
/// cycle; "trigger" captures the region under the pane and sends it to the
/// local Ollama server for combined OCR + translation.
/// </summary>
public partial class MainWindow : Window
{
    public PaneState State { get; private set; } = PaneState.Closed;

    private OllamaClient _ollama = BuildOllamaClient();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        OutputContainer.SizeChanged += (_, _) => AutoFitText();

        var cfg = AppConfig.Current;
        if (cfg.PaneLeft is double left && cfg.PaneTop is double top &&
            cfg.PaneWidth is double w && cfg.PaneHeight is double h)
        {
            var visible = ScreenGeometry.EnsureVisible(
                new Rect(left, top, w, h), CurrentMonitorBounds(), PrimaryWorkingArea());
            Left = visible.Left;
            Top = visible.Top;
            Width = visible.Width;
            Height = visible.Height;
        }
    }

    private static List<Rect> CurrentMonitorBounds() =>
        System.Windows.Forms.Screen.AllScreens
            .Select(s => new Rect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height))
            .ToList();

    private static Rect PrimaryWorkingArea()
    {
        var wa = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
        return new Rect(wa.X, wa.Y, wa.Width, wa.Height);
    }

    private static OllamaClient BuildOllamaClient() =>
        new(AppConfig.Current.OllamaEndpoint, TimeSpan.FromSeconds(AppConfig.Current.TimeoutSeconds));

    /// <summary>Called after settings are saved: endpoint/timeout are baked into
    /// the HttpClient, so it must be rebuilt; model/target language are read
    /// fresh from AppConfig.Current on every request already.</summary>
    public void ApplySettings() => _ollama = BuildOllamaClient();

    /// <summary>Advances the open -> trigger -> close cycle by one step.</summary>
    public void CycleState()
    {
        switch (State)
        {
            case PaneState.Closed:
                ResetForOpen();
                Visibility = Visibility.Visible;
                Show();
                Activate();
                State = PaneState.Open;
                break;

            case PaneState.Open:
                _ = RunTriggerAsync();
                break;

            case PaneState.Busy:
                _cts?.Cancel();
                Hide();
                State = PaneState.Closed;
                break;

            case PaneState.Triggered:
                Hide();
                State = PaneState.Closed;
                break;
        }
    }

    private void ResetForOpen()
    {
        OutputText.Text = string.Empty;
        ContentBackdrop.Visibility = Visibility.Collapsed;
        BusyPanel.Visibility = Visibility.Collapsed;
        CopyButton.Visibility = Visibility.Collapsed;
    }

    private async Task RunTriggerAsync()
    {
        State = PaneState.Busy;
        OutputText.Text = string.Empty;
        ContentBackdrop.Visibility = Visibility.Visible;
        BusyPanel.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();
        try
        {
            var pngBytes = ScreenCapture.CapturePaneRegion(this);
            var translation = await _ollama.TranslateImageAsync(
                pngBytes, AppConfig.Current.ModelName, AppConfig.Current.TargetLanguage,
                AppConfig.Current.TranslationMode, _cts.Token);

            var trimmed = translation.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.Contains(OllamaClient.NoTextSentinel, StringComparison.OrdinalIgnoreCase))
            {
                // Grouped with the failure state per the brief: stays open, retryable.
                OutputText.Text = "No text detected here. Reposition the pane, then press the hotkey or click the pane to retry.";
                State = PaneState.Open;
            }
            else
            {
                OutputText.Text = trimmed;
                State = PaneState.Triggered;
                CopyButton.Visibility = Visibility.Visible;
            }
        }
        catch (OperationCanceledException) when (_cts is { IsCancellationRequested: true })
        {
            // Cancelled by a hotkey press while busy; CycleState's Busy branch
            // already hid the pane and reset state. Nothing more to do.
        }
        catch (OperationCanceledException)
        {
            // Not our own cancellation — e.g. HttpClient's request timeout elapsed.
            // A real failure, not a silent user-initiated cancel: without this,
            // the pane would be left stuck in Busy with no text and no Copy button.
            OutputText.Text = "Translation timed out. Press the hotkey or click the pane to retry.";
            State = PaneState.Open;
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Translation failed: {ex.Message}\nPress the hotkey or click the pane to retry.";
            State = PaneState.Open;
        }
        finally
        {
            BusyPanel.Visibility = Visibility.Collapsed;
            AutoFitText();
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Picks the largest font size (within reason) whose wrapped text still
    /// fits the output container, so the translation fills the available
    /// space instead of leaving it mostly empty.
    /// </summary>
    private void AutoFitText()
    {
        const double minFontSize = 8;
        const double maxFontSize = 72;

        var width = OutputContainer.ActualWidth;
        var height = OutputContainer.ActualHeight;
        if (width <= 0 || height <= 0 || string.IsNullOrEmpty(OutputText.Text))
        {
            return;
        }

        bool Fits(double fontSize)
        {
            OutputText.FontSize = fontSize;
            OutputText.Measure(new System.Windows.Size(width, double.PositiveInfinity));
            return OutputText.DesiredSize.Height <= height;
        }

        double low = minFontSize, high = maxFontSize;
        if (!Fits(low))
        {
            OutputText.FontSize = low;
            return;
        }

        while (high - low > 0.5)
        {
            var mid = (low + high) / 2;
            if (Fits(mid))
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        OutputText.FontSize = low;
    }

    /// <summary>
    /// A drag repositions the pane (DragMove blocks until mouse-up, so comparing
    /// position before/after tells us whether one actually happened); a click with
    /// no movement triggers capture+translate instead, replacing the old [go] button.
    /// </summary>
    private void Chrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        var (startLeft, startTop) = (Left, Top);
        DragMove();

        var moved = Math.Abs(Left - startLeft) > 1.0 || Math.Abs(Top - startTop) > 1.0;
        if (!moved && State == PaneState.Open)
        {
            _ = RunTriggerAsync();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        State = PaneState.Closed;
    }

    /// <summary>Copies the translated text, closes the pane the same way the
    /// close button does, then shows a brief confirmation where the pane was
    /// — a flash inside the small pane itself, right before closing, was too
    /// hard to read.</summary>
    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText(OutputText.Text);

        var centerX = Left + Width / 2;
        var centerY = Top + Height / 2;

        Hide();
        State = PaneState.Closed;

        new CopiedToast(centerX, centerY).Show();
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseButton_Click(sender, e);
        }
    }
}
