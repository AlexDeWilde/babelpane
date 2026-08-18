using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BabelPane;

/// <summary>
/// How faithfully the model should render the translation: a complete,
/// sentence-by-sentence rendering in natural target-language phrasing
/// (nothing summarized or omitted) versus an interpreted/summarized one.
/// </summary>
public enum TranslationMode
{
    Literal,
    Summary,
}

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
        byte[] pngBytes, string model, string targetLanguage, TranslationMode mode, CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = BuildPrompt(mode, targetLanguage),
            ["images"] = new[] { Convert.ToBase64String(pngBytes) },
            ["stream"] = false,
        };
        if (mode == TranslationMode.Literal)
        {
            // Deterministic output for the literal mode: the same region should
            // translate the same way every time, with no creative variance.
            payload["options"] = new { temperature = 0 };
        }

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("/api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return ExtractResponseText(body);
    }

    /// <summary>Builds the OCR+translate prompt for the given mode.</summary>
    public static string BuildPrompt(TranslationMode mode, string targetLanguage) => mode switch
    {
        TranslationMode.Literal =>
            $"Perform OCR on this image. Translate the text into {targetLanguage}, sentence by sentence, translating every " +
            "sentence completely and accurately — do not summarize, condense, omit details, interpret, or add your own opinion " +
            "or commentary. At the same time, write natural, fluent, grammatically correct " +
            $"{targetLanguage} for each sentence: reorder words and phrases as needed so the sentence reads the way a fluent " +
            $"{targetLanguage} speaker would write it, rather than a stilted word-for-word rendering that copies the source " +
            "language's word order. Preserve the exact meaning and level of detail; do not preserve the source's sentence " +
            "structure or word order. Output only the translated text, preserving paragraph breaks. Do not add labels or the " +
            "original text, and do not bracket alternate word choices or add any uncertainty markers or meta-commentary — " +
            "write clean, plain translated text only. " +
            $"If the image contains no legible text at all, respond with exactly this and nothing else: {NoTextSentinel}",

        _ =>
            $"Perform OCR on this image and translate all the text you find into {targetLanguage}. " +
            "Output only the translated text, preserving paragraph breaks. " +
            "Do not add commentary, labels, or the original text, and do not bracket alternate word choices or add any " +
            "uncertainty markers — write clean, plain translated text only. " +
            $"If the image contains no legible text at all, respond with exactly this and nothing else: {NoTextSentinel}",
    };

    /// <summary>Extracts the "response" field from an Ollama /api/generate reply body.</summary>
    public static string ExtractResponseText(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("response").GetString() ?? string.Empty;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new InvalidOperationException("Ollama returned an unexpected response format.", ex);
        }
    }
}
