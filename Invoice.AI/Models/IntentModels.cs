using System.Text.Json.Serialization;

namespace Invoice.AI.Models;

public enum AskMode
{
    /// <summary>Exactly one business question per request.</summary>
    Single,

    /// <summary>One or more business questions in the same request.</summary>
    Multi
}

public sealed class AskRequest
{
    public string Question { get; set; } = string.Empty;
}

/// <summary>One intent exactly as the LLM returned it (untrusted until validated in C#).</summary>
public sealed class ExtractedIntent
{
    [JsonPropertyName("intent")] public string Intent { get; set; } = "Unknown";

    /// <summary>The words of the user's question that belong to this intent.</summary>
    [JsonPropertyName("phrase")] public string Phrase { get; set; } = string.Empty;

    [JsonPropertyName("categoryName")] public string CategoryName { get; set; } = string.Empty;
    [JsonPropertyName("city")] public string City { get; set; } = string.Empty;

    /// <summary>"any" | "active" | "inactive"</summary>
    [JsonPropertyName("activeFilter")] public string ActiveFilter { get; set; } = "any";
}

public sealed class ExtractionResponse
{
    [JsonPropertyName("intents")] public List<ExtractedIntent> Intents { get; set; } = new();
}

public sealed record ExtractionResult(
    IReadOnlyList<ExtractedIntent> Intents,
    string RawOutput,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Cities);

/// <summary>Validated, C#-checked arguments handed to an intent handler.</summary>
public sealed record IntentArguments(
    string? Phrase,
    string? CategoryName,
    string? City,
    bool? IsActive);

public sealed record CityCount(string City, int Count);
