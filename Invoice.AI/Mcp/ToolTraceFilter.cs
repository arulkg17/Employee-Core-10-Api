using System.Diagnostics;

namespace Invoice.AI.Mcp;

/// <summary>
/// A minimal runtime trace container for tool calls. The full Semantic Kernel
/// invocation-filter implementation was removed to avoid compile-time
/// dependency on a specific Semantic Kernel API version. When the project is
/// built against a compatible Semantic Kernel, a separate adapter can call
/// RecordCall to populate the trace.
/// </summary>
public sealed class ToolTraceFilter
{
    private readonly List<ToolCallInfo> _calls = new();

    public IReadOnlyList<ToolCallInfo> Calls
    {
        get { lock (_calls) return _calls.ToList(); }
    }

    /// <summary>Record a tool call. Intended to be invoked by an adapter when the runtime SK API is available.</summary>
    public void RecordCall(string toolName, Dictionary<string, string?> arguments, long elapsedMs)
    {
        lock (_calls) _calls.Add(new ToolCallInfo(toolName, arguments, elapsedMs));
    }
}

public sealed record ToolCallInfo(string Tool, Dictionary<string, string?> Arguments, long ElapsedMs);
