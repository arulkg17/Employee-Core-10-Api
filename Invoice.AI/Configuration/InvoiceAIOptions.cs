namespace Invoice.AI.Configuration;

public sealed class InvoiceAIOptions
{
    public const string SectionName = "InvoiceAI";

    /// <summary>true = fake in-memory data (no SQL Server needed) - great for first demo and unit tests.</summary>
    public bool UseInMemoryData { get; set; }

    /// <summary>true = response includes raw model output and extracted intents.</summary>
    public bool IncludeDebugInfo { get; set; } = true;
}
