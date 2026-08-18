using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BabelPane;

/// <summary>
/// Minimal client for Ollama's /api/generate, used here for combined
/// OCR + translation against a vision-capable model.
/// </summary>
public sealed class OllamaClient
{
    /// <summary>
    /// Sentinel the model is instructed to return verbatim when the image has
    /// no legible text. An empty/whitespace response can't be relied on for
    /// this — a blank image sometimes makes the model echo prompt fragments
    /// instead of returning nothing.
    /// </summary>
    public const string NoTextSentinel = "NO_TEXT_FOUND";

    private readonly HttpClient _http;

    public OllamaClient(string baseUrl, TimeSpan timeout)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = timeout,
        };
    }

    public async Task<string> TranslateImageAsync(
        byte[] pngBytes, string model, string targetLanguage, CancellationToken ct = default)
    {
        var prompt =
            $"Perform OCR on this image and translate all the text you find into {targetLanguage}. " +
            "Output only the translated text, preserving paragraph breaks. " +
            "Do not add commentary, labels, or the original text. " +
            $"If the image contains no legible text at all, respond with exactly this and nothing else: {NoTextSentinel}";

        var payload = new
        {
            model,
            prompt,
            images = new[] { Convert.ToBase64String(pngBytes) },
            stream = false,
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return ExtractResponseText(body);
    }

    /// <summary>Extracts the "response" field from an Ollama /api/generate reply body.</summary>
    public static string ExtractResponseText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }
}
