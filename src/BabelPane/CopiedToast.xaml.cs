namespace BabelPane;

/// <summary>
/// A brief, non-interactive "Copied" confirmation shown after the pane closes
/// (a flash inside the small pane itself, right before closing, was too hard
/// to read). Centered on where the pane was, auto-dismisses on its own.
/// </summary>
public partial class CopiedToast : System.Windows.Window
{
    private readonly double _centerX;
    private readonly double _centerY;

    public CopiedToast(double centerX, double centerY)
    {
        InitializeComponent();
        _centerX = centerX;
        _centerY = centerY;
    }

    private async void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Left = _centerX - ActualWidth / 2;
        Top = _centerY - ActualHeight / 2;

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        Close();
    }
}
