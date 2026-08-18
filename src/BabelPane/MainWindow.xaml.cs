using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BabelPane;

public enum PaneState
{
    Closed,
    Open,
    Triggered,
}

/// <summary>
/// The floating overlay pane. For M1, "trigger" is a stub: no capture or
/// translation call happens yet (that lands in M2) — it just advances the
/// hotkey's 3-state cycle and reports what would happen.
/// </summary>
public partial class MainWindow : Window
{
    public PaneState State { get; private set; } = PaneState.Closed;

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Advances the open -> trigger -> close cycle by one step.</summary>
    public void CycleState()
    {
        switch (State)
        {
            case PaneState.Closed:
                StatusText.Text = "open — press the hotkey to trigger (stub)";
                Visibility = Visibility.Visible;
                Show();
                Activate();
                State = PaneState.Open;
                break;

            case PaneState.Open:
                TriggerStub();
                State = PaneState.Triggered;
                break;

            case PaneState.Triggered:
                Hide();
                State = PaneState.Closed;
                break;
        }
    }

    private void TriggerStub()
    {
        // M2 replaces this with: capture the region under Chrome, call Ollama,
        // render the translation. For now it only proves the cycle advances.
        StatusText.Text = "triggered (stub — no capture/translate yet)\npress the hotkey to close";
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
