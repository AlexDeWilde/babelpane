namespace BabelPane;

/// <summary>
/// Hardcoded defaults for M2. A settings window (endpoint, model, target
/// language, timeout, hotkey) is a later milestone — these constants are the
/// seeded values until that lands.
/// </summary>
public static class AppConfig
{
    public const string OllamaEndpoint = "http://192.168.68.52:11434";
    public const string ModelName = "gemma4-e4b-110k:latest";
    public const string TargetLanguage = "English";
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);
}
