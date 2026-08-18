using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace BabelPane;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private HotkeyManager? _hotkeyManager;
    private NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();

        _hotkeyManager = new HotkeyManager(_mainWindow);
        _hotkeyManager.Pressed += () => _mainWindow!.CycleState();
        RegisterHotkeyFromSettings();
        AppConfig.SettingsChanged += OnSettingsChanged;

        _trayIcon = CreateTrayIcon();
    }

    private void RegisterHotkeyFromSettings()
    {
        var cfg = AppConfig.Current;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(cfg.HotkeyKey);
        if (!_hotkeyManager!.Register(cfg.HotkeyModifiers, vk))
        {
            System.Windows.MessageBox.Show(
                $"Could not register the global hotkey ({cfg.HotkeyModifiers}+{cfg.HotkeyKey}). It may already be in use.",
                "BabelPane", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnSettingsChanged()
    {
        RegisterHotkeyFromSettings();
        _mainWindow?.ApplySettings();
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open widget pane", null, (_, _) => OpenWidgetPane());
        menu.Items.Add("Open settings", null, (_, _) => OpenSettingsWindow());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        var icon = new NotifyIcon
        {
            Icon = TrayIconFactory.CreateChiliIcon(),
            Text = "BabelPane",
            Visible = true,
            ContextMenuStrip = menu,
        };
        icon.DoubleClick += (_, _) => OpenWidgetPane();
        return icon;
    }

    private void OpenWidgetPane()
    {
        if (_mainWindow!.State == PaneState.Closed)
        {
            _mainWindow.CycleState();
        }
        else
        {
            _mainWindow.Activate();
        }
    }

    private void OpenSettingsWindow()
    {
        new SettingsWindow().ShowDialog();
    }

    private void ExitApplication()
    {
        SaveGeometry();
        _trayIcon!.Visible = false;
        _trayIcon.Dispose();
        _hotkeyManager?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveGeometry();
        _trayIcon?.Dispose();
        _hotkeyManager?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Persists pane size/position (never its content) so it reopens in the
    /// same place after an app restart, regardless of whether the pane
    /// itself was open or closed at the time.
    /// </summary>
    private void SaveGeometry()
    {
        if (_mainWindow == null)
        {
            return;
        }

        var cfg = AppConfig.Current;
        cfg.PaneLeft = _mainWindow.Left;
        cfg.PaneTop = _mainWindow.Top;
        cfg.PaneWidth = _mainWindow.Width;
        cfg.PaneHeight = _mainWindow.Height;
        cfg.SaveGeometry();
    }
}
