using System.Net.Http.Json;
using Invoice.RAG.Configuration;
using Invoice.RAG.Models;
using Microsoft.Extensions.Options;

namespace Invoice.RAG.Ollama;

public sealed class OllamaClient(HttpClient http, IOptions<RagOptions> options, ILogger<OllamaClient> logger)
{
    private OllamaOptions O => options.Value.Ollama;

    /// <summary>Embeds the given texts (callers should send at most ~16 at a time).</summary>
    public async Task<float[][]> EmbedAsync(IReadOnlyList<string> inputs, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("api/embed",
            new { model = O.EmbeddingModel, input = inputs, keep_alive = O.KeepAlive }, ct);

        await EnsureOkAsync(response, O.EmbeddingModel, ct);

        var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
        if (body?.Embeddings is null || body.Embeddings.Length != inputs.Count)
            throw new RagException(502, "Ollama returned an unexpected embedding response.");

        return body.Embeddings;
    }

    /// <summary>One chat call. json = true forces valid JSON output (used for term extraction).</summary>
    public async Task<string> ChatAsync(string system, string user, bool json, CancellationToken ct)
    {
        var request = new Dictionary<string, object?>
        {
            ["model"] = O.ChatModel,
            ["stream"] = false,
            ["keep_alive"] = O.KeepAlive,
            ["messages"] = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            },
            ["options"] = new { temperature = 0, num_ctx = O.NumCtx }
        };
        if (json) request["format"] = "json";

        var started = DateTime.UtcNow;
        using var response = await http.PostAsJsonAsync("api/chat", request, ct);
        await EnsureOkAsync(response, O.ChatModel, ct);

        var body = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
        logger.LogInformation("Ollama chat ({Model}) took {Ms} ms", O.ChatModel, (DateTime.UtcNow - started).TotalMilliseconds);

        return body?.Message?.Content ?? throw new RagException(502, "Ollama returned an empty answer.");
    }

    private static async Task EnsureOkAsync(HttpResponseMessage response, string model, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var detail = await response.Content.ReadAsStringAsync(ct);
        if (detail.Length > 200) detail = detail[..200];
        throw new RagException(502,
            $"Ollama returned HTTP {(int)response.StatusCode}. Is the model '{model}' pulled? (ollama pull {model}). {detail}");
    }

    private sealed record EmbedResponse(float[][]? Embeddings);
    private sealed record ChatResponse(ChatMessage? Message);
    private sealed record ChatMessage(string? Content);
}
