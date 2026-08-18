using System.Windows;
using System.Windows.Controls.Primitives;
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
            Left = left;
            Top = top;
            Width = w;
            Height = h;
        }
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
                pngBytes, AppConfig.Current.ModelName, AppConfig.Current.TargetLanguage, _cts.Token);

            var trimmed = translation.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.Contains(OllamaClient.NoTextSentinel, StringComparison.OrdinalIgnoreCase))
            {
                // Grouped with the failure state per the brief: stays open, retryable.
                OutputText.Text = "No text detected here. Reposition the pane, then press the hotkey or [go] to retry.";
                State = PaneState.Open;
            }
            else
            {
                OutputText.Text = trimmed;
                State = PaneState.Triggered;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled by a hotkey press while busy; CycleState's Busy branch
            // already hid the pane and reset state. Nothing more to do.
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Translation failed: {ex.Message}\nPress the hotkey or [go] to retry.";
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

    private void GoButton_Click(object sender, RoutedEventArgs e)
    {
        if (State == PaneState.Open)
        {
            _ = RunTriggerAsync();
        }
    }

    private void Chrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var newWidth = Width + e.HorizontalChange;
        var newHeight = Height + e.VerticalChange;
        if (newWidth >= MinWidth) Width = newWidth;
        if (newHeight >= MinHeight) Height = newHeight;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        State = PaneState.Closed;
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseButton_Click(sender, e);
        }
    }
}
