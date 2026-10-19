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
    public CoreApiEndpoints CoreApiEndpoints { get; set; } = new();
    public AgentOptions Agent { get; set; } = new();
}

/// <summary>Limits for the tool-calling agent (POST api/v1/agent/ask).</summary>
public sealed class AgentOptions
{
    /// <summary>Model round trips per request (each one may call several tools).</summary>
    public int MaxSteps { get; set; } = 6;
    public int MaxToolCallsPerStep { get; set; } = 6;
    public int MaxQuestions { get; set; } = 8;
    /// <summary>One tool result is cut at this size.</summary>
    public int MaxToolResultChars { get; set; } = 6000;
    /// <summary>All tool results of one request together. Keep it well below NumCtx (about 4 chars per token).</summary>
    public int MaxTotalToolChars { get; set; } = 14000;
}

/// <summary>
/// CoreAPI routes used by the agent tools. Check them against your Swagger and adjust in appsettings
/// (Rag:CoreApiEndpoints). {vendorId} is replaced by the vendor id.
/// </summary>
public sealed class CoreApiEndpoints
{
    public string VendorList { get; set; } = "api/v1/Vendor/GetAllPaged?pageNumber=1&pageSize=200";
    public string PurchaseOrdersByVendor { get; set; } = "api/v1/PurchaseOrder/GetAllPaged?VendorId={vendorId}&pageNumber=1&pageSize=200";
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
