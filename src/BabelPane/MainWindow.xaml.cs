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

    private readonly OllamaClient _ollama = new(AppConfig.OllamaEndpoint, AppConfig.RequestTimeout);

    public MainWindow()
    {
        InitializeComponent();
        OutputContainer.SizeChanged += (_, _) => AutoFitText();
    }

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
                // Mid-request cancel is a later milestone; ignored for now.
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

        try
        {
            var pngBytes = ScreenCapture.CapturePaneRegion(this);
            var translation = await _ollama.TranslateImageAsync(pngBytes, AppConfig.ModelName, AppConfig.TargetLanguage);
            OutputText.Text = string.IsNullOrWhiteSpace(translation) ? "(no text detected)" : translation.Trim();
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Translation failed: {ex.Message}";
        }
        finally
        {
            BusyPanel.Visibility = Visibility.Collapsed;
            State = PaneState.Triggered;
            AutoFitText();
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
}
