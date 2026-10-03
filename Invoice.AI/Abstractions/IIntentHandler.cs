using Invoice.AI.Models;

namespace Invoice.AI.Abstractions;

/// <summary>
/// One business capability (e.g. "count active customers").
/// To teach the AI a NEW question type, just add a new handler class and register it -
/// the prompt and the JSON schema are generated from the registered handlers.
/// </summary>
public interface IIntentHandler
{
    string IntentName { get; }

    /// <summary>One or two sentences shown to the LLM so it knows when to pick this intent.</summary>
    string Description { get; }

    Task<IntentExecutionResult> HandleAsync(IntentArguments args, CancellationToken ct);
}
