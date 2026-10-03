using System.Net.Http.Json;
using System.Text.Json;
using Invoice.AI.Abstractions;
using Invoice.AI.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Invoice.AI.Ollama;

/// <summary>
/// Talks to Ollama's native /api/chat endpoint.
/// The "format" field receives a JSON schema, so the model is FORCED to return valid JSON
/// in exactly our shape - this is what makes a 7B model reliable.
/// </summary>
public sealed class OllamaClient : IOllamaClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options, ILogger<OllamaClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ChatJsonAsync(
        string systemPrompt,
        string userMessage,
        object jsonSchema,
        CancellationToken ct = default)
    {
        var body = new
        {
            model = _options.Model,
            stream = false,
            format = jsonSchema,
            keep_alive = _options.KeepAlive,
            options = new { temperature = 0, num_ctx = _options.NumCtx },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            }
        };

        _logger.LogDebug("Calling Ollama model {Model}", _options.Model);

        using var response = await _http.PostAsJsonAsync("api/chat", body, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Ollama returned {(int)response.StatusCode}: {raw}");
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        LogTimings(root);

        return root
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    /// <summary>
    /// Ollama reports durations in nanoseconds. This shows WHERE the time goes:
    /// load   = model being loaded into memory (should be ~0 after the first call)
    /// prompt = reading our system prompt + question
    /// output = generating the JSON answer
    /// </summary>
    private void LogTimings(JsonElement root)
    {
        static double Seconds(JsonElement r, string name) =>
            r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt64() / 1_000_000_000.0
                : 0;

        static long Number(JsonElement r, string name) =>
            r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt64()
                : 0;

        var promptTokens = Number(root, "prompt_eval_count");
        var promptSec = Seconds(root, "prompt_eval_duration");
        var outTokens = Number(root, "eval_count");
        var outSec = Seconds(root, "eval_duration");

        var outRate = outSec > 0 ? outTokens / outSec : 0;

        var model = root.TryGetProperty("model", out var m) ? m.GetString() : "?";

        _logger.LogInformation(
            "Ollama timings [{Model}]: total {Total:F1}s | load {Load:F1}s | prompt {PromptTokens} tokens in {PromptSec:F1}s | output {OutTokens} tokens in {OutSec:F1}s ({Rate:F1} tokens/sec)",
            model,
            Seconds(root, "total_duration"),
            Seconds(root, "load_duration"),
            promptTokens,
            promptSec,
            outTokens,
            outSec,
            outRate);
    }
}