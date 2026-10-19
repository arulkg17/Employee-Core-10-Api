using System.Net.Http.Json;
using System.Text.Json;
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

    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>
    /// One chat turn with tools (Ollama native tool calling). The model either answers (Content)
    /// or asks for one or more tool calls. The caller runs the tools and sends the results back.
    /// </summary>
    public async Task<ChatTurn> ChatWithToolsAsync(
        IReadOnlyList<Dictionary<string, object?>> messages, IReadOnlyList<object>? tools, CancellationToken ct)
    {
        var request = new Dictionary<string, object?>
        {
            ["model"] = O.ChatModel,
            ["stream"] = false,
            ["keep_alive"] = O.KeepAlive,
            ["messages"] = messages,
            ["options"] = new { temperature = 0, num_ctx = O.NumCtx }
        };
        if (tools is { Count: > 0 }) request["tools"] = tools;

        var started = DateTime.UtcNow;
        using var response = await http.PostAsJsonAsync("api/chat", request, ct);
        await EnsureOkAsync(response, O.ChatModel, ct);

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        logger.LogInformation("Ollama chat+tools ({Model}) took {Ms} ms", O.ChatModel, (DateTime.UtcNow - started).TotalMilliseconds);

        if (!doc.RootElement.TryGetProperty("message", out var message))
            throw new RagException(502, "Ollama returned an empty answer.");

        var content = message.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
            ? c.GetString() ?? ""
            : "";

        var calls = new List<ToolCall>();
        if (message.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in tc.EnumerateArray())
            {
                if (call.TryGetProperty("function", out var fn) &&
                    fn.TryGetProperty("name", out var n) &&
                    n.GetString() is { Length: > 0 } name)
                    calls.Add(new ToolCall(name, ReadArguments(fn)));
            }
        }

        // Some 7B models print the call as JSON text instead of using tool_calls: accept that too.
        if (calls.Count == 0 && TryInlineToolCall(content, out var inline))
        {
            calls.Add(inline);
            content = "";
        }

        return new ChatTurn(content, calls);
    }

    private static JsonElement ReadArguments(JsonElement function)
    {
        if (!function.TryGetProperty("arguments", out var a)) return EmptyObject;

        if (a.ValueKind == JsonValueKind.Object) return a.Clone();

        if (a.ValueKind == JsonValueKind.String && a.GetString() is { } text)
        {
            try
            {
                using var parsed = JsonDocument.Parse(text);
                if (parsed.RootElement.ValueKind == JsonValueKind.Object) return parsed.RootElement.Clone();
            }
            catch (JsonException) { /* fall through */ }
        }
        return EmptyObject;
    }

    private static bool TryInlineToolCall(string content, out ToolCall call)
    {
        call = null!;
        var text = content.Trim();
        if (text.Length < 10 || text[0] != '{' || text[^1] != '}') return false;

        try
        {
            using var parsed = JsonDocument.Parse(text);
            var root = parsed.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("arguments", out var a) || a.ValueKind != JsonValueKind.Object)
                return false;

            call = new ToolCall(n.GetString()!, a.Clone());
            return true;
        }
        catch (JsonException) { return false; }
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

/// <summary>A tool the model wants to run. Arguments is always a JSON object.</summary>
public sealed record ToolCall(string Name, JsonElement Arguments)
{
    public string ArgumentsJson => Arguments.GetRawText();
}

/// <summary>One model turn: a final answer (Content) or tool calls to run first.</summary>
public sealed record ChatTurn(string Content, IReadOnlyList<ToolCall> ToolCalls);
