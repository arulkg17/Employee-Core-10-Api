namespace Invoice.RAG.Models;

/// <summary>Send "question" (one message, may contain several questions) and/or "questions" (a list).</summary>
public sealed class AgentRequest
{
    public string? Question { get; set; }
    public List<string>? Questions { get; set; }
}

/// <summary>What the agent did: one entry per tool call (handy for debugging and for showing "how I got this").</summary>
public sealed record ToolTrace(string Tool, string Arguments, long ElapsedMs, bool Failed, bool Cached);

public sealed class AgentResponse
{
    public string Answer { get; set; } = "";
    /// <summary>Agreement excerpts the answer may cite as [1], [2]...</summary>
    public List<SourceRef> Sources { get; set; } = [];
    public List<ToolTrace> ToolCalls { get; set; } = [];
    public int Steps { get; set; }
    public long ElapsedMs { get; set; }
}
