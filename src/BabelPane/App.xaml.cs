using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace BabelPane;

public partial class App : Application
{
    // Fixed, app-specific name so a second launch attempt can detect the
    // first instance is already running; distinct from BabelPaneSky's so
    // the two products can run side by side.
    private const string SingleInstanceMutexName = "BabelPane-SingleInstance-9F1E7B9E-2E1B-4F1D-8B0B-6F1A9C6DB2B4";

    private MainWindow? _mainWindow;
    private HotkeyManager? _hotkeyManager;
    private NotifyIcon? _trayIcon;
    private Mutex? _singleInstanceMutex;
    private bool _isPrimaryInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out _isPrimaryInstance);
        if (!_isPrimaryInstance)
        {
            System.Windows.MessageBox.Show(
                "BabelPane is already running. Check your system tray.",
                "BabelPane", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _mainWindow = new MainWindow();

        _hotkeyManager = new HotkeyManager(_mainWindow);
        _hotkeyManager.Pressed += () => _mainWindow!.CycleState();
        RegisterHotkeyFromSettings();
        AppConfig.SettingsChanged += OnSettingsChanged;

        _trayIcon = CreateTrayIcon();

        OpenSettingsWindow(autoCloseOnFirstLaunch: true);
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

    private void OpenSettingsWindow(bool autoCloseOnFirstLaunch = false)
    {
        new SettingsWindow(autoCloseOnFirstLaunch).ShowDialog();
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
        if (_isPrimaryInstance)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        _singleInstanceMutex?.Dispose();
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
