namespace Invoice.AI.Models;

public sealed class IntentExecutionResult
{
    public string Intent { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Answer { get; init; } = string.Empty;
    public object? Data { get; init; }

    public static IntentExecutionResult Ok(string intent, string answer, object? data = null)
        => new() { Intent = intent, Success = true, Answer = answer, Data = data };

    public static IntentExecutionResult Fail(string intent, string answer)
        => new() { Intent = intent, Success = false, Answer = answer };
}

public sealed class AIAnswer
{
    public string Question { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;

    /// <summary>All answers joined into one readable sentence block.</summary>
    public string Answer { get; init; } = string.Empty;

    /// <summary>One entry per question that was understood (useful for the UI / tests).</summary>
    public IReadOnlyList<IntentExecutionResult> Results { get; init; } = Array.Empty<IntentExecutionResult>();

    public long ElapsedMs { get; init; }

    /// <summary>Raw model output + extracted intents. Only filled when InvoiceAI:IncludeDebugInfo = true.</summary>
    public object? Debug { get; init; }
}
