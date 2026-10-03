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
        return doc.RootElement
                   .GetProperty("message")
                   .GetProperty("content")
                   .GetString() ?? string.Empty;
    }
}
