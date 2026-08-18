using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace BabelPane;

/// <summary>
/// User-editable settings (endpoint, model, target language, timeout, hotkey)
/// plus persisted pane geometry. Loaded once as <see cref="Current"/> and
/// saved to a JSON file under %AppData%\BabelPane.
/// </summary>
public sealed class AppConfig
{
    public string OllamaEndpoint { get; set; } = "http://192.168.68.52:11434";
    public string ModelName { get; set; } = "gemma4-e4b-110k:latest";
    public string TargetLanguage { get; set; } = "English";
    public int TimeoutSeconds { get; set; } = 60;
    public HotkeyModifiers HotkeyModifiers { get; set; } = HotkeyModifiers.Win | HotkeyModifiers.Alt;
    public Key HotkeyKey { get; set; } = Key.X;

    // Pane geometry, persisted across restarts; the pane's content never is.
    public double? PaneLeft { get; set; }
    public double? PaneTop { get; set; }
    public double? PaneWidth { get; set; }
    public double? PaneHeight { get; set; }

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BabelPane", "settings.json");

    public static AppConfig Current { get; } = Load();

    /// <summary>Raised after a settings (not geometry-only) save, so listeners can re-apply them.</summary>
    public static event Action? SettingsChanged;

    private static AppConfig Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings file: fall back to defaults below.
        }
        return new AppConfig();
    }

    /// <summary>Persists settings and notifies listeners to re-apply them.</summary>
    public void Save()
    {
        WriteToDisk();
        SettingsChanged?.Invoke();
    }

    /// <summary>Persists pane geometry only, without re-triggering settings listeners.</summary>
    public void SaveGeometry() => WriteToDisk();

    private void WriteToDisk()
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
