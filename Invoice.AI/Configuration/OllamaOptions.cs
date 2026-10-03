namespace Invoice.AI.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "InvoiceAI:Ollama";

    /// <summary>Plain Ollama address - do NOT add /v1 here (native /api/chat is used).</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "qwen2.5:7b-instruct";

    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>Keeps the model loaded in memory between calls (avoids the slow first-load every time).</summary>
    public string KeepAlive { get; set; } = "30m";

    public int NumCtx { get; set; } = 4096;
}
