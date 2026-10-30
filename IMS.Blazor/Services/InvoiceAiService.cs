using System.Text.Json;

namespace IMS.Blazor.Services;

/// <summary>
/// Invoice.AI endpoints exposed by Invoice.CoreAPI:
///   POST api/invoice-ai/ask        (single intent)
///   POST api/invoice-ai/ask-multi  (multiple intents in one question)
/// ASSUMED request body: { question }. If your controller expects another shape, change it here.
/// </summary>
public class InvoiceAiService
{
    private readonly ApiClient _api;

    public InvoiceAiService(ApiClient api) => _api = api;

    public async Task<string> AskAsync(string question, bool multiIntent)
    {
        var path = multiIntent ? "invoice-ai/ask-multi" : "invoice-ai/ask";
        var result = await _api.PostAsync(path, new { question });

        if (!result.Success)
        {
            throw new ApiException(result.MessageOr("The AI service did not answer. Is Ollama running?"));
        }

        if (result.Data is { } data && data.ValueKind != JsonValueKind.Null)
        {
            return ExtractAnswer(data);
        }

        return string.IsNullOrWhiteSpace(result.RawText) ? result.Message : result.RawText;
    }

    /// <summary>Pulls the readable answer out of whatever the API returned.</summary>
    private static string ExtractAnswer(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            // "message" is checked last because ApiResponse.message is usually just "Success"
            foreach (var key in new[] { "answer", "data", "result", "response", "message" })
            {
                if (value.TryGetProperty(key, out var inner) && inner.ValueKind != JsonValueKind.Null)
                {
                    return ExtractAnswer(inner);
                }
            }
        }

        return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
    }
}
