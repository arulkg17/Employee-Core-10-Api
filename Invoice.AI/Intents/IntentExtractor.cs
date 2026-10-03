using System.Text.Json;
using Invoice.AI.Abstractions;
using Invoice.AI.Models;
using Microsoft.Extensions.Logging;

namespace Invoice.AI.Intents;

/// <summary>Step 1 of the pipeline: question text -> list of ExtractedIntent (via Ollama).</summary>
public sealed class IntentExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IOllamaClient _ollama;
    private readonly IMasterDataQueryService _data;
    private readonly IntentCatalog _catalog;
    private readonly IntentPromptBuilder _prompts;
    private readonly ILogger<IntentExtractor> _logger;

    public IntentExtractor(
        IOllamaClient ollama,
        IMasterDataQueryService data,
        IntentCatalog catalog,
        IntentPromptBuilder prompts,
        ILogger<IntentExtractor> logger)
    {
        _ollama = ollama;
        _data = data;
        _catalog = catalog;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractAsync(string question, AskMode mode, CancellationToken ct = default)
    {
        var categories = await _data.GetCategoryNamesAsync(ct);
        var cities = await _data.GetCityNamesAsync(ct);

        var systemPrompt = _prompts.BuildSystemPrompt(mode, _catalog.Handlers, categories, cities);
        var schema = _prompts.BuildSchema(mode, _catalog.IntentNames);

        var raw = await _ollama.ChatJsonAsync(systemPrompt, question.Trim(), schema, ct);

        _logger.LogInformation("Ollama raw output for [{Question}]: {Raw}", question, raw);

        return new ExtractionResult(Parse(raw), raw, categories, cities);
    }

    private List<ExtractedIntent> Parse(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<ExtractionResponse>(raw, JsonOptions)?.Intents ?? new();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Model output was not valid JSON, trying to recover.");
        }

        // Recovery: keep only the text between the first '{' and the last '}'
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            try
            {
                return JsonSerializer.Deserialize<ExtractionResponse>(raw[start..(end + 1)], JsonOptions)?.Intents ?? new();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Recovery failed as well.");
            }
        }

        return new();
    }
}
