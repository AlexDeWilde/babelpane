using System.Windows;
using System.Windows.Forms;
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
        // Default hotkey per DECISIONS.md: Win+Alt+X. VK_X = 0x58.
        if (!_hotkeyManager.Register(HotkeyModifiers.Win | HotkeyModifiers.Alt, 0x58))
        {
            System.Windows.MessageBox.Show(
                "Could not register the global hotkey (Win+Alt+X). It may already be in use.",
                "BabelPane", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _trayIcon = CreateTrayIcon();
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open widget pane", null, (_, _) => OpenWidgetPane());
        menu.Items.Add("Open settings", null, (_, _) => OpenSettingsStub());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        var icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
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

    private void OpenSettingsStub()
    {
        System.Windows.MessageBox.Show(
            "Settings window is not implemented yet (planned for a later milestone).",
            "BabelPane", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitApplication()
    {
        _trayIcon!.Visible = false;
        _trayIcon.Dispose();
        _hotkeyManager?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _hotkeyManager?.Dispose();
        base.OnExit(e);
    }
}
