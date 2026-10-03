using Invoice.AI.Models;

namespace Invoice.AI.Abstractions;

/// <summary>
/// Entry point of Invoice.AI. The API controller only talks to this interface.
/// </summary>
public interface IAIOrchestrator
{
    Task<AIAnswer> AskAsync(string question, AskMode mode, CancellationToken ct = default);
}
