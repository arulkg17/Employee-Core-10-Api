using Invoice.AI.Mcp;

namespace Invoice.AI.Vendor;

/// <summary>Send "question" (one message, may hold several questions) and/or "questions" (a list).</summary>
public sealed class VendorAskRequest
{
    public string? Question { get; set; }
    public List<string>? Questions { get; set; }
}

public sealed class VendorAskResult
{
    public string Answer { get; set; } = "";
    /// <summary>Every tool the model called: name, arguments, time. Shows HOW the answer was produced.</summary>
    public IReadOnlyList<ToolCallInfo> ToolCalls { get; set; } = [];
    public List<string> ToolsAvailable { get; set; } = [];
    public bool SkillLoaded { get; set; }
    public long ElapsedMs { get; set; }
}

public sealed class ImsMcpOptions
{
    /// <summary>URL of IMS.MCP.Server (add the path if the server uses MapMcp("/mcp")).</summary>
    public string McpEndpoint { get; set; } = "http://localhost:3001/";
    /// <summary>Maximum iterations of model->toolCalls->model loop. Prevents runaway loops.</summary>
    public int MaxIterations { get; set; } = 3;
    /// <summary>Maximum total tool calls allowed for one request.</summary>
    public int MaxToolCalls { get; set; } = 16;
}
