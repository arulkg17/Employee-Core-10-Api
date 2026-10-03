using System.Diagnostics;
using Invoice.AI.Abstractions;
using Invoice.AI.Configuration;
using Invoice.AI.Intents;
using Invoice.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Invoice.AI.Services;

/// <summary>
/// The whole pipeline in one place (read this file first when teaching):
///
///   1. LLM  : question  -> JSON intents          (understanding)
///   2. C#   : validate  -> fix spelling, active/inactive, unknown intents
///   3. C#   : handler   -> database count        (facts)
///   4. C#   : build the final sentence(s)        (wording)
///
/// The LLM never sees database data and never writes the final answer.
/// </summary>
public sealed class AIOrchestrator : IAIOrchestrator
{
    public const string UnsupportedMessage = "I don't currently support that type of business question.";
    public const string SingleModeNote = "(I answered only the first question. Use the multi-question endpoint to ask several at once.)";

    private readonly IntentExtractor _extractor;
    private readonly IntentCatalog _catalog;
    private readonly InvoiceAIOptions _options;
    private readonly ILogger<AIOrchestrator> _logger;

    public AIOrchestrator(
        IntentExtractor extractor,
        IntentCatalog catalog,
        IOptions<InvoiceAIOptions> options,
        ILogger<AIOrchestrator> logger)
    {
        _extractor = extractor;
        _catalog = catalog;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AIAnswer> AskAsync(string question, AskMode mode, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // STEP 1 - LLM understands the question
        var extraction = await _extractor.ExtractAsync(question, mode, ct);
        var intents = extraction.Intents.ToList();

        var trimmed = false;
        if (mode == AskMode.Single && intents.Count > 1)
        {
            intents = intents.Take(1).ToList();
            trimmed = true;
        }

        var results = new List<IntentExecutionResult>();

        if (intents.Count == 0)
            results.Add(IntentExecutionResult.Fail(IntentNames.Unknown, UnsupportedMessage));

        foreach (var extracted in intents)
        {
            if (!_catalog.TryGet(extracted.Intent, out var handler))
            {
                results.Add(IntentExecutionResult.Fail(IntentNames.Unknown, UnsupportedMessage));
                continue;
            }

            // STEP 2 - C# validates what the LLM returned
            var args = BuildArguments(question, extracted, extraction);

            // STEP 3 + 4 - C# gets the facts and writes the sentence
            try
            {
                results.Add(await handler.HandleAsync(args, ct));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Intent} failed", handler.IntentName);
                results.Add(IntentExecutionResult.Fail(
                    handler.IntentName, "I hit a problem while reading that data."));
            }
        }

        var answer = string.Join(" ", results.Select(r => r.Answer).Distinct());
        if (trimmed)
            answer += " " + SingleModeNote;

        stopwatch.Stop();

        _logger.LogInformation(
            "AI answered in {Elapsed} ms ({Mode}): {Answer}", stopwatch.ElapsedMilliseconds, mode, answer);

        return new AIAnswer
        {
            Question = question,
            Mode = mode.ToString(),
            Answer = answer,
            Results = results,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            Debug = _options.IncludeDebugInfo
                ? new { rawModelOutput = extraction.RawOutput, extractedIntents = intents }
                : null
        };
    }

    private static IntentArguments BuildArguments(string question, ExtractedIntent extracted, ExtractionResult context)
    {
        var category = CleanValue(extracted.CategoryName);
        if (category != null)
            category = FuzzyMatcher.BestMatch(category, context.Categories) ?? category;

        var city = CleanValue(extracted.City);
        if (city != null)
            city = FuzzyMatcher.BestMatch(city, context.Cities) ?? city;

        var isActive = ActiveFilterResolver.Resolve(question, extracted.Phrase, extracted.ActiveFilter);

        return new IntentArguments(extracted.Phrase, category, city, isActive);
    }

    /// <summary>Small models sometimes write "null" or "none" as text instead of leaving the value empty.</summary>
    private static string? CleanValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var v = value.Trim();
        return v.ToLowerInvariant() is "null" or "none" or "n/a" or "na" or "unknown" ? null : v;
    }
}
