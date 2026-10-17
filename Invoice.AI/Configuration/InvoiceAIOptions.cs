namespace Invoice.AI.Configuration;

public sealed class InvoiceAIOptions
{
    public const string SectionName = "InvoiceAI";

    /// <summary>
    /// true = fake in-memory data (no database needed) - great for first demo and unit tests.
    /// false = real data through the BAL (IMasterCountService).
    /// </summary>
    public bool UseInMemoryData { get; set; }

    /// <summary>true = response includes raw model output and extracted intents.</summary>
    public bool IncludeDebugInfo { get; set; } = true;
}
