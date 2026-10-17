namespace Invoice.RAG.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>"InMemory" (demo, data lost on restart) or "Sql" (needs ConnectionStrings:RagDb).</summary>
    public string StoreProvider { get; set; } = "Sql";

    /// <summary>Folder where the uploaded PDFs are kept (never served by path, only by agreement id).</summary>
    public string StoragePath { get; set; } = "data/agreements";

    public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 150;
    public int TopK { get; set; } = 6;
    public double MinScore { get; set; } = 0.25;

    /// <summary>Upper limit of agreement text sent to the model. Keep it below NumCtx (about 4 chars per token).</summary>
    public int MaxContextChars { get; set; } = 9000;

    public OllamaOptions Ollama { get; set; } = new();
    public PoFieldMap PoFields { get; set; } = new();
}

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "qwen2.5:7b-instruct";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int TimeoutSeconds { get; set; } = 300;
    public string KeepAlive { get; set; } = "30m";
    public int NumCtx { get; set; } = 8192;

    // nomic-embed-text works best with these task prefixes. Set both to "" for other embedding models.
    public string DocumentPrefix { get; set; } = "search_document: ";
    public string QueryPrefix { get; set; } = "search_query: ";
}

/// <summary>
/// Possible JSON property names for the PO fields (case-insensitive, first match wins).
/// Adjust here if your PurchaseOrderDto uses other names.
/// </summary>
public sealed class PoFieldMap
{
    public string[] Id { get; set; } = ["id"];
    public string[] PoNumber { get; set; } = ["poNumber", "poNo"];
    public string[] PoDate { get; set; } = ["poDate", "orderDate", "date", "createdDate"];
    public string[] VendorId { get; set; } = ["vendorId"];
    public string[] Status { get; set; } = ["status"];
    public string[] Amount { get; set; } = ["totalAmount", "grandTotal", "netAmount", "total", "amount"];
    public string[] Currency { get; set; } = ["currency", "currencyCode"];
}
