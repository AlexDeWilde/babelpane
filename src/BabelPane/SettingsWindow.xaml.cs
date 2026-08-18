using System.Windows;
using System.Windows.Input;

namespace BabelPane;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var cfg = AppConfig.Current;
        EndpointBox.Text = cfg.OllamaEndpoint;
        ModelBox.Text = cfg.ModelName;
        LanguageBox.Text = cfg.TargetLanguage;
        TimeoutBox.Text = cfg.TimeoutSeconds.ToString();
        WinCheck.IsChecked = cfg.HotkeyModifiers.HasFlag(HotkeyModifiers.Win);
        CtrlCheck.IsChecked = cfg.HotkeyModifiers.HasFlag(HotkeyModifiers.Control);
        AltCheck.IsChecked = cfg.HotkeyModifiers.HasFlag(HotkeyModifiers.Alt);
        ShiftCheck.IsChecked = cfg.HotkeyModifiers.HasFlag(HotkeyModifiers.Shift);
        KeyBox.Text = cfg.HotkeyKey.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var endpoint = EndpointBox.Text.Trim();
        var model = ModelBox.Text.Trim();
        var language = LanguageBox.Text.Trim();
        var keyText = KeyBox.Text.Trim().ToUpperInvariant();

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            ValidationText.Text = "Enter a valid endpoint URL (e.g. http://192.168.1.10:11434).";
            return;
        }
        if (string.IsNullOrWhiteSpace(model))
        {
            ValidationText.Text = "Model name cannot be empty.";
            return;
        }
        if (string.IsNullOrWhiteSpace(language))
        {
            ValidationText.Text = "Target language cannot be empty.";
            return;
        }
        if (!int.TryParse(TimeoutBox.Text.Trim(), out var timeoutSeconds) || timeoutSeconds <= 0)
        {
            ValidationText.Text = "Timeout must be a positive whole number of seconds.";
            return;
        }
        if (keyText.Length != 1 || keyText[0] < 'A' || keyText[0] > 'Z')
        {
            ValidationText.Text = "Hotkey key must be a single letter (A-Z).";
            return;
        }

        var modifiers = HotkeyModifiers.None;
        if (WinCheck.IsChecked == true) modifiers |= HotkeyModifiers.Win;
        if (CtrlCheck.IsChecked == true) modifiers |= HotkeyModifiers.Control;
        if (AltCheck.IsChecked == true) modifiers |= HotkeyModifiers.Alt;
        if (ShiftCheck.IsChecked == true) modifiers |= HotkeyModifiers.Shift;

        if (modifiers == HotkeyModifiers.None)
        {
            ValidationText.Text = "Select at least one modifier key.";
            return;
        }

        var cfg = AppConfig.Current;
        cfg.OllamaEndpoint = endpoint;
        cfg.ModelName = model;
        cfg.TargetLanguage = language;
        cfg.TimeoutSeconds = timeoutSeconds;
        cfg.HotkeyModifiers = modifiers;
        cfg.HotkeyKey = (Key)Enum.Parse(typeof(Key), keyText);
        cfg.Save();

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
